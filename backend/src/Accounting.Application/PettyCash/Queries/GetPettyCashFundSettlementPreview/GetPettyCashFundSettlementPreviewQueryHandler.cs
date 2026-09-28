using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundSettlementPreview;

/// <summary>
/// Read-only preview of the current computable settlement period — exactly the numbers
/// <c>FinalizePettyCashSettlementCommandHandler</c> would enforce, computed the same way (same
/// <see cref="IPettyCashSettlementPeriodProvisioner"/>/<see cref="IPettyCashSettlementReadRepository"/>
/// calls) so the two can never disagree. Never stages or saves anything — uses the write-side
/// <see cref="IPettyCashFundRepository"/>/<see cref="IPettyCashSettlementPeriodProvisioner"/>
/// purely for their read-only lookups (no <see cref="IUnitOfWork.SaveChangesAsync"/> is ever
/// called from a query handler).
/// </summary>
public sealed class GetPettyCashFundSettlementPreviewQueryHandler
    : IRequestHandler<GetPettyCashFundSettlementPreviewQuery, PettyCashSettlementPreviewDto?>
{
    private readonly IPettyCashFundRepository _fundRepository;
    private readonly IPettyCashSettlementPeriodProvisioner _provisioner;
    private readonly IPettyCashSettlementReadRepository _settlementReadRepository;
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IVoucherTafsiliLevelGuard _tafsiliLevelGuard;

    public GetPettyCashFundSettlementPreviewQueryHandler(
        IPettyCashFundRepository fundRepository,
        IPettyCashSettlementPeriodProvisioner provisioner,
        IPettyCashSettlementReadRepository settlementReadRepository,
        IPettyCashExpenseDocRepository expenseDocRepository,
        IVoucherTafsiliLevelGuard tafsiliLevelGuard)
    {
        _fundRepository = fundRepository;
        _provisioner = provisioner;
        _settlementReadRepository = settlementReadRepository;
        _expenseDocRepository = expenseDocRepository;
        _tafsiliLevelGuard = tafsiliLevelGuard;
    }

    public async Task<PettyCashSettlementPreviewDto?> Handle(
        GetPettyCashFundSettlementPreviewQuery request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepository.GetForUpdateAsync(request.FundId, request.VahedCode, cancellationToken);

        if (fund is null)
        {
            return null;
        }

        var period = await _provisioner.ComputeCurrentAsync(fund, request.VahedCode, cancellationToken);

        var movement = await _settlementReadRepository.GetPeriodMovementAsync(
            fund.ID, request.VahedCode, period.PeriodStart, period.PeriodEnd, cancellationToken);

        var exposure = await _expenseDocRepository.GetFundExposureAsync(
            fund.ID, request.VahedCode, excludeDocId: null, cancellationToken);

        var fundTafsilis = await _settlementReadRepository.GetFundTafsilisAsync(fund.ID, request.VahedCode, cancellationToken);

        var closingCashBalance = period.OpeningBalance + movement.ReplenishmentsAndRefunds - movement.ApprovedExpensesTotal;

        var lines = movement.ExpenseGroups
            .Select(g => new PettyCashSettlementVoucherLineDto(
                g.AccountCodeId, g.AccountCode, g.AccountTitle, g.Tafsilis, g.Amount, 0m))
            .ToList();

        if (movement.ExpenseGroups.Count > 0)
        {
            lines.Add(new PettyCashSettlementVoucherLineDto(
                fund.ACCOUNTCODE_ID, fund.ACCOUNTCODE?.ACCCODE, fund.ACCOUNTCODE?.ACCCODENAME,
                fundTafsilis, 0m, movement.ApprovedExpensesTotal));
        }

        var totalDebtor = movement.ApprovedExpensesTotal;
        var totalCredit = movement.ApprovedExpensesTotal;

        var voucherPreview = new PettyCashSettlementVoucherPreviewDto(
            period.PeriodEnd,
            $"تسویه تنخواه {fund.NAME} — دوره {period.PeriodStart} تا {period.PeriodEnd}",
            lines,
            totalDebtor,
            totalCredit,
            totalDebtor == totalCredit);

        var checks = await BuildChecksAsync(fund, period, movement, exposure, fundTafsilis, cancellationToken);

        return new PettyCashSettlementPreviewDto(
            period.PeriodId,
            period.PeriodStart,
            period.PeriodEnd,
            period.State,
            period.OpeningBalance,
            movement.ReplenishmentsAndRefunds,
            movement.ApprovedExpensesTotal,
            exposure.InFlightAmount,
            exposure.InFlightCount,
            closingCashBalance,
            period.CountedBalance,
            voucherPreview,
            checks,
            movement.DocIds);
    }

    private async Task<IReadOnlyList<PettyCashSettlementCheckDto>> BuildChecksAsync(
        TB_PC_FUND fund,
        PettyCashSettlementPeriodSnapshot period,
        PettyCashSettlementMovementDto movement,
        PettyCashFundExposure exposure,
        IReadOnlyList<PettyCashSettlementTafsiliDto> fundTafsilis,
        CancellationToken cancellationToken)
    {
        var checks = new List<PettyCashSettlementCheckDto>
        {
            new("hasApprovedDocs", movement.DocIds.Count > 0,
                movement.DocIds.Count > 0
                    ? $"{movement.DocIds.Count} سند برای تسویه آماده است."
                    : "هیچ سند تأییدشده‌ای برای تسویه نیست."),

            new("fundAccountConfigured", fund.ACCOUNTCODE_ID is not null,
                fund.ACCOUNTCODE_ID is not null ? "حساب معین تنخواه تنظیم شده است." : "ابتدا حساب معین تنخواه را تعریف کنید."),

            new("countedBalanceRecorded", period.CountedBalance is not null,
                period.CountedBalance is not null ? "شمارش صندوق ثبت شده است." : "هنوز شمارش صندوق ثبت نشده است."),
        };

        var closingCashBalance = period.OpeningBalance + movement.ReplenishmentsAndRefunds - movement.ApprovedExpensesTotal;

        if (period.CountedBalance is { } counted)
        {
            checks.Add(new PettyCashSettlementCheckDto(
                "countedBalanceMatches",
                counted == closingCashBalance,
                counted == closingCashBalance
                    ? "مبلغ شمارش‌شده با مانده نقد محاسبه‌شده برابر است."
                    : $"مبلغ شمارش‌شده ({counted}) با مانده نقد محاسبه‌شده ({closingCashBalance}) برابر نیست."));
        }

        checks.Add(new PettyCashSettlementCheckDto(
            "inFlightAcknowledged",
            exposure.InFlightCount == 0,
            exposure.InFlightCount == 0
                ? "سند در جریانی وجود ندارد."
                : $"{exposure.InFlightCount} سند در جریان است؛ برای نهایی‌سازی باید acknowledgeInFlightTransfer=true ارسال شود."));

        var tafsiliOk = true;
        var tafsiliMessage = "تفصیلی همهٔ ردیف‌های سند تسویه معتبر است.";

        try
        {
            foreach (var group in movement.ExpenseGroups)
            {
                if (group.AccountCodeId is not { } accountCodeId)
                {
                    tafsiliOk = false;
                    tafsiliMessage = "یکی از ماده‌های هزینهٔ منظورشده حساب معین تعریف‌شده ندارد.";
                    break;
                }

                await _tafsiliLevelGuard.EnsureSatisfiedAsync(
                    accountCodeId,
                    group.Tafsilis.Select(t => new VoucherDetailTafsiliLinkInput(t.TafsiliId, t.LevelId)).ToList(),
                    cancellationToken);
            }

            if (tafsiliOk && fund.ACCOUNTCODE_ID is { } fundAccountCodeId)
            {
                await _tafsiliLevelGuard.EnsureSatisfiedAsync(
                    fundAccountCodeId,
                    fundTafsilis.Select(t => new VoucherDetailTafsiliLinkInput(t.TafsiliId, t.LevelId)).ToList(),
                    cancellationToken);
            }
        }
        catch (TafsiliLevelRuleException ex)
        {
            tafsiliOk = false;
            tafsiliMessage = ex.PublicDetail;
        }

        checks.Add(new PettyCashSettlementCheckDto("voucherLinesTafsiliValid", tafsiliOk, tafsiliMessage));

        return checks;
    }
}
