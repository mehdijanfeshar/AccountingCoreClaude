using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Common;
using Accounting.Application.PettyCash.Queries;
using Accounting.Application.Treasury.Queries;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>See <see cref="ITreasuryDashboardReadRepository"/> XML doc.</summary>
public sealed class TreasuryDashboardReadRepository : ITreasuryDashboardReadRepository
{
    /// <summary>Same bounded fetch cap <c>GetApprovalCartableQueryHandler.ReplenishmentFetchCap</c>
    /// uses, for the same reason — a دشبورد/کارتابل-sized list, never unbounded.</summary>
    private const int ReplenishmentFetchCap = 500;

    private const int OpenItemsTop = 10;
    private const int CommitmentsWindowDays = 7;
    private const int AlertWindowDays = 3;

    private readonly LegacyDbContext _dbContext;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly ITreasuryBankAccountBalanceReadRepository _balanceReadRepository;
    private readonly IPettyCashReplenishmentReadRepository _replenishmentReadRepository;

    public TreasuryDashboardReadRepository(
        LegacyDbContext dbContext,
        IBankAccountReadRepository bankAccountReadRepository,
        ITreasuryBankAccountBalanceReadRepository balanceReadRepository,
        IPettyCashReplenishmentReadRepository replenishmentReadRepository)
    {
        _dbContext = dbContext;
        _bankAccountReadRepository = bankAccountReadRepository;
        _balanceReadRepository = balanceReadRepository;
        _replenishmentReadRepository = replenishmentReadRepository;
    }

    public async Task<TreasuryDashboardDto> GetAsync(string vahedCode, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var today = PettyCashSettlementPeriodCalculator.ToJalaliString(now);
        var commitmentsEnd = PettyCashSettlementPeriodCalculator.ToJalaliString(now.AddDays(CommitmentsWindowDays));
        var alertEnd = PettyCashSettlementPeriodCalculator.ToJalaliString(now.AddDays(AlertWindowDays));
        var year = today[..4];

        // ---- ۱) موجودی بانک‌ها — bounded per-unit loop (documented, mirrors کارتابل's per-fund
        // reviewer lookup precedent — never large enough to warrant a bulk query). ----
        var bankAccountsPage = await _bankAccountReadRepository.GetPagedAsync(1, 200, vahedCode, cancellationToken);

        var bankAccounts = new List<TreasuryDashboardBankAccountDto>();
        var totalBankBalance = 0m;

        foreach (var account in bankAccountsPage.Items)
        {
            if (account.AccountCodeId is not { } accountCodeId)
            {
                continue;
            }

            var tafsiliIds = account.TafsiliLinks.Select(l => l.TafsiliId).ToList();
            var balance = await _balanceReadRepository.GetBalanceAsync(
                accountCodeId, tafsiliIds, vahedCode, year, cancellationToken: cancellationToken);

            bankAccounts.Add(new TreasuryDashboardBankAccountDto(account.Id, account.AccountNumber, balance));
            totalBankBalance += balance;
        }

        // ---- ۲) درخواست‌های پرداخت — یک کوئری خام، همهٔ محاسبات بعدی در حافظه. ----
        var paymentRequests = await _dbContext.TB_TR_PAYMENT_REQUESTs.AsNoTracking()
            .Where(p => !p.ISDELETED && p.VAHEDCODE == vahedCode)
            .Select(p => new
            {
                p.ID,
                p.CODE,
                p.BENEFICIARY_NAME,
                p.NET_PAYABLE_AMOUNT,
                p.REQUEST_STATE,
                p.DUE_DATE,
                p.PAID_DATE,
                p.CREATEDDATE,
            })
            .ToListAsync(cancellationToken);

        var commitments = paymentRequests
            .Where(p => (p.REQUEST_STATE == PaymentRequestState.ReadyForExecution || p.REQUEST_STATE == PaymentRequestState.Suspended)
                        && string.Compare(p.DUE_DATE, today) >= 0 && string.Compare(p.DUE_DATE, commitmentsEnd) <= 0)
            .ToList();

        var commitmentsAmount = commitments.Sum(p => p.NET_PAYABLE_AMOUNT);

        var commitmentsDto = new TreasuryDashboardCommitmentsDto(
            commitments.Count, commitmentsAmount, commitmentsAmount == 0 ? null : totalBankBalance / commitmentsAmount);

        var dueSoonCount = paymentRequests.Count(p =>
            (p.REQUEST_STATE == PaymentRequestState.ReadyForExecution || p.REQUEST_STATE == PaymentRequestState.Suspended)
            && string.Compare(p.DUE_DATE, today) >= 0 && string.Compare(p.DUE_DATE, alertEnd) <= 0);

        // ---- ۳) انتقال‌های وجه. ----
        var transfers = await _dbContext.TB_TR_TRANSFERs.AsNoTracking()
            .Where(t => !t.ISDELETED && t.VAHEDCODE == vahedCode)
            .Select(t => new { t.ID, t.CODE, t.AMOUNT, t.STATE, t.CREATEDDATE })
            .ToListAsync(cancellationToken);

        var pendingTransfers = transfers.Where(t => t.STATE == TransferState.PendingTreasurer).ToList();

        // ---- ۴) ترمیم‌های تنخواه — از رپازیتوری موجود، همان کوئری «پیشنهاد» کارتابل. ----
        var replenishmentPage = await _replenishmentReadRepository.GetPagedAsync(
            pageNumber: 1, pageSize: ReplenishmentFetchCap, fundId: null, state: null, vahedCode: vahedCode, cancellationToken: cancellationToken);

        var pendingReplenishments = replenishmentPage.Items
            .Where(r => r.State == PettyCashReplenishmentState.PendingTreasurer)
            .ToList();

        // ---- ۵) کارتابل تأیید — همان سه منبع، فقط مجموع/شمار (نه «برای من»). ----
        var pendingPaymentRequests = paymentRequests
            .Where(p => p.REQUEST_STATE is PaymentRequestState.PendingUnitManager
                        or PaymentRequestState.PendingFinanceManager or PaymentRequestState.PendingCeo)
            .ToList();

        var pendingApprovalCount = pendingPaymentRequests.Count + pendingTransfers.Count + pendingReplenishments.Count;
        var pendingApprovalAmount = pendingPaymentRequests.Sum(p => p.NET_PAYABLE_AMOUNT)
            + pendingTransfers.Sum(t => t.AMOUNT) + pendingReplenishments.Sum(r => r.TotalAmount);

        var pendingApprovalDto = new TreasuryDashboardPendingApprovalDto(pendingApprovalCount, pendingApprovalAmount);

        // ---- ۶) دریافت‌ها. ----
        var receipts = await _dbContext.TB_TR_RECEIPTs.AsNoTracking()
            .Where(r => !r.ISDELETED && r.VAHEDCODE == vahedCode)
            .Select(r => new { r.ID, r.CODE, r.PAYER_NAME, r.AMOUNT, r.STATE, r.RECEIPT_DATE, r.CREATEDDATE })
            .ToListAsync(cancellationToken);

        var todayReceiptsAmount = receipts
            .Where(r => r.STATE == ReceiptState.Registered && r.RECEIPT_DATE == today)
            .Sum(r => r.AMOUNT);

        var todayPaymentsAmount = paymentRequests
            .Where(p => p.REQUEST_STATE == PaymentRequestState.Executed && p.PAID_DATE == today)
            .Sum(p => p.NET_PAYABLE_AMOUNT);

        var todayDto = new TreasuryDashboardTodayDto(todayReceiptsAmount, todayPaymentsAmount, todayReceiptsAmount - todayPaymentsAmount);

        // ---- ۷) openItems — چهار منبع، قدیمی‌ترین ۱۰ تای اول. ----
        var openCandidates = new List<(DateTime CreatedDate, TreasuryDashboardOpenItemDto Item)>();

        foreach (var p in paymentRequests.Where(p => p.REQUEST_STATE != PaymentRequestState.Executed && p.REQUEST_STATE != PaymentRequestState.Rejected))
        {
            openCandidates.Add((p.CREATEDDATE, new TreasuryDashboardOpenItemDto(
                "payment", p.ID, p.CODE, p.BENEFICIARY_NAME, p.NET_PAYABLE_AMOUNT, p.DUE_DATE, PaymentRequestStateLabel(p.REQUEST_STATE))));
        }

        foreach (var r in receipts.Where(r => r.STATE == ReceiptState.Draft))
        {
            openCandidates.Add((r.CREATEDDATE, new TreasuryDashboardOpenItemDto(
                "receipt", r.ID, r.CODE, r.PAYER_NAME, r.AMOUNT, null, ReceiptStateLabel(r.STATE))));
        }

        foreach (var t in transfers.Where(t => t.STATE != TransferState.Executed && t.STATE != TransferState.Rejected))
        {
            openCandidates.Add((t.CREATEDDATE, new TreasuryDashboardOpenItemDto(
                "transfer", t.ID, t.CODE, t.CODE, t.AMOUNT, null, TransferStateLabel(t.STATE))));
        }

        foreach (var r in replenishmentPage.Items.Where(r => r.State != PettyCashReplenishmentState.Paid && r.State != PettyCashReplenishmentState.Rejected))
        {
            openCandidates.Add((r.CreatedDate, new TreasuryDashboardOpenItemDto(
                "replenishment", r.Id, r.Code, r.FundName, r.TotalAmount, null, ReplenishmentStateLabel(r.State))));
        }

        var openItems = openCandidates
            .OrderBy(x => x.CreatedDate)
            .Take(OpenItemsTop)
            .Select(x => x.Item)
            .ToList();

        // ---- ۸) هشدارها. ----
        var alerts = new List<string>();

        if (dueSoonCount > 0)
        {
            alerts.Add($"{dueSoonCount} پرداخت طی {AlertWindowDays} روز آینده سررسید دارد.");
        }

        return new TreasuryDashboardDto(
            totalBankBalance, bankAccounts, commitmentsDto, pendingApprovalDto, todayDto, openItems, alerts);
    }

    private static string PaymentRequestStateLabel(PaymentRequestState state) => state switch
    {
        PaymentRequestState.Draft => "پیش‌نویس",
        PaymentRequestState.PendingUnitManager => "در انتظار تأیید مدیر واحد",
        PaymentRequestState.PendingFinanceManager => "در انتظار تأیید مدیر مالی",
        PaymentRequestState.PendingCeo => "در انتظار تأیید مدیرعامل",
        PaymentRequestState.ReadyForExecution => "آمادهٔ اجرا",
        PaymentRequestState.Returned => "برگشتی",
        PaymentRequestState.Rejected => "ردشده",
        PaymentRequestState.Executed => "اجراشده",
        PaymentRequestState.Suspended => "معلق",
        _ => "نامشخص",
    };

    private static string ReceiptStateLabel(ReceiptState state) => state switch
    {
        ReceiptState.Draft => "پیش‌نویس",
        ReceiptState.Registered => "ثبت‌شده",
        ReceiptState.Cancelled => "لغوشده",
        _ => "نامشخص",
    };

    private static string TransferStateLabel(TransferState state) => state switch
    {
        TransferState.Draft => "پیش‌نویس",
        TransferState.PendingTreasurer => "در انتظار اقدام خزانه‌دار",
        TransferState.Executed => "اجراشده",
        TransferState.Returned => "برگشتی",
        TransferState.Rejected => "ردشده",
        _ => "نامشخص",
    };

    private static string ReplenishmentStateLabel(PettyCashReplenishmentState state) => state switch
    {
        PettyCashReplenishmentState.Draft => "پیش‌نویس",
        PettyCashReplenishmentState.PendingFinanceManager => "در انتظار تأیید مدیر مالی",
        PettyCashReplenishmentState.PendingTreasurer => "در انتظار اقدام خزانه‌دار",
        PettyCashReplenishmentState.Paid => "پرداخت‌شده",
        PettyCashReplenishmentState.Rejected => "ردشده",
        _ => "نامشخص",
    };
}
