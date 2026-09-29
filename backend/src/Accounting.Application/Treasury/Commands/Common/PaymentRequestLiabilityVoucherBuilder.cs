using System.Globalization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// See <see cref="IPaymentRequestLiabilityVoucherBuilder"/> XML doc.
///
/// <b>Doc-num reservation cache.</b> Registered Scoped (one instance per HTTP request), it
/// memoises the last <c>DOC_NUM</c> it handed out per (vahedCode, year) for the lifetime of the
/// request — exactly the same reason <c>VoucherTafsiliLevelGuard</c> memoises تفصیلی levels per
/// معین. Without this, <c>bulk-approve</c> transitioning several requests to
/// <see cref="PaymentRequestState.ReadyForExecution"/> in the same call would have every one of
/// them call <c>IVoucherHeadRepository.GetNextDocNumAsync</c> against the database BEFORE any of
/// them is ever saved — since that query is <c>AsNoTracking()</c> against the database, not the
/// in-memory change tracker, every call in the same batch would see the same "next" number and
/// hand out duplicate <c>DOC_NUM</c> values. Reserving locally after the first DB read for a given
/// (vahedCode, year) closes that race for the common case (one bulk-approve call); a race between
/// two *concurrent* requests remains the same accepted, documented gap every other doc-num
/// generator in this project has (no unique DB constraint backs <c>DOC_NUM</c>).
/// </summary>
public sealed class PaymentRequestLiabilityVoucherBuilder : IPaymentRequestLiabilityVoucherBuilder
{
    private const string PayablesAccountLabel = "حساب بستانکاران";
    private const string VatCreditAccountLabel = "حساب اعتبار مالیات بر ارزش‌افزوده";
    private const string InsurancePayableAccountLabel = "حساب بستانکاران بیمه";

    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;
    private readonly ITreasurySettingReadRepository _settingReadRepository;
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly IPaymentRequestPayablesTafsiliResolver _payablesTafsiliResolver;
    private readonly ICurrentUser _currentUser;

    private readonly Dictionary<(string VahedCode, string Year), int> _reservedDocNum = new();

    public PaymentRequestLiabilityVoucherBuilder(
        IVoucherHeadRepository voucherHeadRepository,
        IVoucherDetailRepository voucherDetailRepository,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard,
        ITreasurySettingReadRepository settingReadRepository,
        IPaymentRequestRepository paymentRequestRepository,
        IPaymentRequestPayablesTafsiliResolver payablesTafsiliResolver,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _voucherDetailRepository = voucherDetailRepository;
        _tafsiliLevelGuard = tafsiliLevelGuard;
        _settingReadRepository = settingReadRepository;
        _paymentRequestRepository = paymentRequestRepository;
        _payablesTafsiliResolver = payablesTafsiliResolver;
        _currentUser = currentUser;
    }

    public async Task<PaymentRequestVoucherBuildResult> BuildAndStageAsync(
        TB_TR_PAYMENT_REQUEST paymentRequest, string vahedCode, CancellationToken cancellationToken = default)
    {
        var setting = await _settingReadRepository.GetByVahedAsync(vahedCode, cancellationToken)
            ?? throw new PaymentRequestSettingsMissingException(vahedCode);

        var payablesAccountId = setting.PayablesAccountId
            ?? throw new PaymentRequestTreasurySettingAccountMissingException(PayablesAccountLabel);

        Guid? vatAccountId = null;

        if (paymentRequest.VAT_AMOUNT > 0)
        {
            vatAccountId = setting.VatCreditAccountId
                ?? throw new PaymentRequestTreasurySettingAccountMissingException(VatCreditAccountLabel);
        }

        Guid? insuranceAccountId = null;

        if (paymentRequest.INSURANCE_DEDUCTION_AMOUNT > 0)
        {
            insuranceAccountId = setting.InsurancePayableAccountId
                ?? throw new PaymentRequestTreasurySettingAccountMissingException(InsurancePayableAccountLabel);
        }

        var expenseLinks = (await _paymentRequestRepository.GetActiveCostCenterTafsiliLinksAsync(paymentRequest.ID, cancellationToken))
            .Select(l => new VoucherDetailTafsiliLinkInput(l.TAFSILI_ID, l.LEVEL_ID))
            .ToList();

        var payablesLinks = await _payablesTafsiliResolver.ResolvePayablesLinksAsync(
            payablesAccountId, paymentRequest.ID, paymentRequest.BENEFICIARY_TAFSILI_ID, cancellationToken);

        if (vatAccountId is { } vatId)
        {
            await _payablesTafsiliResolver.EnsureNoTafsiliRequiredAsync(vatId, VatCreditAccountLabel, cancellationToken);
        }

        if (insuranceAccountId is { } insuranceId)
        {
            await _payablesTafsiliResolver.EnsureNoTafsiliRequiredAsync(insuranceId, InsurancePayableAccountLabel, cancellationToken);
        }

        // Always pass the FINAL links through the shared guard, even though the resolver above
        // already derived them from the same source it reads — defense in depth, same posture as
        // PettyCashSettlementVoucherBuilder.
        await EnsureTafsiliSatisfiedAsync(paymentRequest.EXPENSE_ACCOUNT_ID, expenseLinks, cancellationToken);
        await EnsureTafsiliSatisfiedAsync(payablesAccountId, payablesLinks, cancellationToken);

        if (vatAccountId is { } vatIdForGuard)
        {
            await EnsureTafsiliSatisfiedAsync(vatIdForGuard, Array.Empty<VoucherDetailTafsiliLinkInput>(), cancellationToken);
        }

        if (insuranceAccountId is { } insuranceIdForGuard)
        {
            await EnsureTafsiliSatisfiedAsync(insuranceIdForGuard, Array.Empty<VoucherDetailTafsiliLinkInput>(), cancellationToken);
        }

        var dateDoc = TodayJalali();
        var year = dateDoc[..4];
        var docNum = await ReserveNextDocNumAsync(vahedCode, year, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var head = new TB_VOUCHERSHEAD
        {
            ID = Guid.NewGuid(),
            DOC_NUM = docNum,
            DATE_DOC = dateDoc,
            // owner decision ۲۰۲۶-۰۹-۲۹ #۵ — هر دو سند بخش ۴-ب «موقت»‌اند.
            DOCLIFE = DocLife.Temporary,
            HEAD_DESC = $"شناسایی بدهی — {paymentRequest.CODE} — {paymentRequest.BENEFICIARY_NAME}",
            VAHEDCODE = vahedCode,
            YEAR = year,
            ISAUTOMATIC = true,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherHeadRepository.AddAsync(head, cancellationToken);

        var lines = new List<PaymentRequestVoucherLineResult>();
        var radif = 0;

        await AddLineAsync(
            head.ID, ++radif, paymentRequest.EXPENSE_ACCOUNT_ID, "شناسایی بدهی — هزینه",
            debit: paymentRequest.AMOUNT_BEFORE_TAX, credit: 0, expenseLinks, vahedCode, year, now, userId, lines, cancellationToken);

        if (vatAccountId is { } vatAccount)
        {
            await AddLineAsync(
                head.ID, ++radif, vatAccount, "شناسایی بدهی — اعتبار مالیات بر ارزش‌افزوده",
                debit: paymentRequest.VAT_AMOUNT, credit: 0, Array.Empty<VoucherDetailTafsiliLinkInput>(),
                vahedCode, year, now, userId, lines, cancellationToken);
        }

        await AddLineAsync(
            head.ID, ++radif, payablesAccountId, "شناسایی بدهی — بستانکاران",
            debit: 0, credit: paymentRequest.NET_PAYABLE_AMOUNT, payablesLinks, vahedCode, year, now, userId, lines, cancellationToken);

        if (insuranceAccountId is { } insuranceAccount)
        {
            await AddLineAsync(
                head.ID, ++radif, insuranceAccount, "شناسایی بدهی — بستانکاران بیمه",
                debit: 0, credit: paymentRequest.INSURANCE_DEDUCTION_AMOUNT, Array.Empty<VoucherDetailTafsiliLinkInput>(),
                vahedCode, year, now, userId, lines, cancellationToken);
        }

        var totalDebtor = paymentRequest.AMOUNT_BEFORE_TAX + paymentRequest.VAT_AMOUNT;
        var totalCredit = paymentRequest.NET_PAYABLE_AMOUNT + paymentRequest.INSURANCE_DEDUCTION_AMOUNT;

        if (totalDebtor != totalCredit)
        {
            throw new PaymentRequestVoucherUnbalancedException("شناسایی بدهی", totalDebtor, totalCredit);
        }

        return new PaymentRequestVoucherBuildResult(head.ID, docNum, dateDoc, year, lines);
    }

    private async Task AddLineAsync(
        Guid voucherHeadId,
        int radif,
        Guid accountCodeId,
        string description,
        decimal debit,
        decimal credit,
        IReadOnlyList<VoucherDetailTafsiliLinkInput> tafsiliLinks,
        string vahedCode,
        string year,
        DateTime now,
        string userId,
        List<PaymentRequestVoucherLineResult> lines,
        CancellationToken cancellationToken)
    {
        var detail = new TB_VOUCHERSDETAIL
        {
            ID = Guid.NewGuid(),
            VOUCHERSHEAD_ID = voucherHeadId,
            ACCOUNT_ID = accountCodeId,
            DESCRIPTION = description,
            RADIF = radif,
            DEBTOR = debit,
            CREDITOR = credit,
            VAHEDCODE = vahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _voucherDetailRepository.AddAsync(detail, cancellationToken);

        foreach (var tafsili in tafsiliLinks)
        {
            await _voucherDetailRepository.AddTafsiliLinkAsync(
                new TB_VOUCHERDETAIL_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    VOUCHERSDETAIL_ID = detail.ID,
                    TAFSILI_ID = tafsili.TafsiliId,
                    LEVEL_ID = tafsili.LevelId,
                    VAHEDCODE = vahedCode,
                    YEAR = year,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                },
                cancellationToken);
        }

        lines.Add(new PaymentRequestVoucherLineResult(accountCodeId, debit, credit, tafsiliLinks));
    }

    private async Task EnsureTafsiliSatisfiedAsync(
        Guid accountCodeId, IReadOnlyList<VoucherDetailTafsiliLinkInput> tafsiliLinks, CancellationToken cancellationToken)
    {
        try
        {
            await _tafsiliLevelGuard.EnsureSatisfiedAsync(accountCodeId, tafsiliLinks, cancellationToken);
        }
        catch (TafsiliLevelRuleException ex)
        {
            throw new PaymentRequestVoucherTafsiliMissingException(accountCodeId, ex.PublicDetail);
        }
    }

    private async Task<string> ReserveNextDocNumAsync(string vahedCode, string year, CancellationToken cancellationToken)
    {
        var key = (vahedCode, year);

        if (_reservedDocNum.TryGetValue(key, out var last))
        {
            var reserved = last + 1;
            _reservedDocNum[key] = reserved;

            return reserved.ToString("000000", CultureInfo.InvariantCulture);
        }

        var nextDocNum = await _voucherHeadRepository.GetNextDocNumAsync(vahedCode, year, cancellationToken);
        var parsed = int.Parse(nextDocNum, CultureInfo.InvariantCulture);
        _reservedDocNum[key] = parsed;

        return nextDocNum;
    }

    /// <summary>
    /// Today as a Legacy <c>YYYYMMDD</c> Jalali string — voucher 1's date is the approval moment
    /// itself (owner decision ۲۰۲۶-۰۹-۲۹ #۲), not a caller-supplied value. Same approach as
    /// <c>ReverseVoucherCommandHandler.TodayJalali</c>.
    /// </summary>
    private static string TodayJalali()
    {
        var calendar = new PersianCalendar();
        var now = DateTime.Now;

        return $"{calendar.GetYear(now):0000}{calendar.GetMonth(now):00}{calendar.GetDayOfMonth(now):00}";
    }
}
