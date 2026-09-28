using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundDashboard;

/// <summary>
/// Composes <see cref="IPettyCashFundReadRepository"/> (سقف/موجودی نقد/تأییدشدهٔ کل/در جریان —
/// already include بخش ۳-الف's extended balance terms), <see cref="IPettyCashReplenishmentReadRepository.GetPaidSummaryAsync"/>
/// (ترمیم پرداخت‌شدهٔ هنوز‌تسویه‌نشده) and <see cref="IPettyCashRefundReadRepository.GetTotalAsync"/>
/// (استرداد) to build the balance-check block, then reuses <see cref="IPettyCashExpenseDocReadRepository.GetPagedAsync"/>
/// (New/PendingReview/Returned, already oldest-first) for both «کارتابل امروز» and the
/// «بیش از ۵ روز»/«قدیمی‌ترین برگشتی» figures — no new expense-doc query needed.
/// </summary>
public sealed class GetPettyCashFundDashboardQueryHandler : IRequestHandler<GetPettyCashFundDashboardQuery, PettyCashFundDashboardDto?>
{
    // A fund's live in-flight کارتابل is expected to stay small (same assumption
    // PettyCashExpenseDocRepository.GetFundExposureAsync documents) — large enough to compute
    // "older than 5 days"/"oldest برگشتی" accurately without a second, dedicated aggregate query.
    private const int InFlightSampleSize = 500;
    private const int TodayActionsTake = 10;

    private static readonly IReadOnlyList<PettyCashDocState> InFlightStates =
        new[] { PettyCashDocState.New, PettyCashDocState.PendingReview, PettyCashDocState.Returned };

    private readonly IPettyCashFundReadRepository _fundReadRepository;
    private readonly IPettyCashReplenishmentReadRepository _replenishmentReadRepository;
    private readonly IPettyCashRefundReadRepository _refundReadRepository;
    private readonly IPettyCashExpenseDocReadRepository _expenseDocReadRepository;

    public GetPettyCashFundDashboardQueryHandler(
        IPettyCashFundReadRepository fundReadRepository,
        IPettyCashReplenishmentReadRepository replenishmentReadRepository,
        IPettyCashRefundReadRepository refundReadRepository,
        IPettyCashExpenseDocReadRepository expenseDocReadRepository)
    {
        _fundReadRepository = fundReadRepository;
        _replenishmentReadRepository = replenishmentReadRepository;
        _refundReadRepository = refundReadRepository;
        _expenseDocReadRepository = expenseDocReadRepository;
    }

    public async Task<PettyCashFundDashboardDto?> Handle(GetPettyCashFundDashboardQuery request, CancellationToken cancellationToken)
    {
        var fund = await _fundReadRepository.GetByIdAsync(request.FundId, request.VahedCode, cancellationToken);

        if (fund is null)
        {
            return null;
        }

        var paidSummary = await _replenishmentReadRepository.GetPaidSummaryAsync(request.FundId, request.VahedCode, cancellationToken);
        var refundTotal = await _refundReadRepository.GetTotalAsync(request.FundId, request.VahedCode, cancellationToken);

        var inFlightPage = await _expenseDocReadRepository.GetPagedAsync(
            1,
            InFlightSampleSize,
            new PettyCashExpenseDocFilter(FundId: request.FundId, States: InFlightStates),
            request.VahedCode,
            cancellationToken);

        var inFlightItems = inFlightPage.Page.Items;

        var awaitingReplenishment = new PettyCashDashboardBucketDto(
            fund.ApprovedAmount - paidSummary.Amount, fund.ApprovedCount - paidSummary.DocCount);

        var olderThan5DaysCount = inFlightItems.Count(i => (i.AgeDays ?? 0) > 5);

        var returnedItems = inFlightItems.Where(i => i.State == PettyCashDocState.Returned).ToList();
        var returnedOldestAgeDays = returnedItems.Count == 0
            ? (int?)null
            : returnedItems.Max(i => i.AgeDays ?? 0);

        // بخش ۳-الف — extended equation; see PettyCashDashboardBalanceCheckDto XML doc for the
        // full derivation. Computed from the unsimplified form so it also verifies
        // paidSummary.Amount truly equals the fund's own replenishedNotSettled figure.
        var balanced = fund.CashBalance + fund.ApprovedAmount + fund.InFlightAmount
            - paidSummary.Amount - refundTotal == fund.Ceiling;

        var cashPercentOfCeiling = fund.Ceiling == 0m ? 0m : fund.CashBalance / fund.Ceiling * 100m;
        var belowAlertThreshold = fund.AlertThresholdPercent is { } threshold && cashPercentOfCeiling < threshold;

        var alerts = new List<PettyCashDashboardAlertDto>();

        if (belowAlertThreshold)
        {
            alerts.Add(new PettyCashDashboardAlertDto("warning", "موجودی نقد تنخواه از آستانهٔ هشدار کمتر است."));
        }

        if (olderThan5DaysCount > 0)
        {
            alerts.Add(new PettyCashDashboardAlertDto("warning", $"{olderThan5DaysCount} سند بیش از ۵ روز در کارتابل مانده است."));
        }

        var todayActions = inFlightItems
            .Take(TodayActionsTake)
            .Select(i => new PettyCashDashboardActionItemDto(
                i.Id, i.DocNumber, fund.CustodianName, i.Description, i.TotalAmount, i.State, i.AgeDays ?? 0))
            .ToList();

        return new PettyCashFundDashboardDto(
            new PettyCashFundSummaryDto(fund.Id, fund.Code, fund.Name, fund.Ceiling, fund.AlertThresholdPercent),
            fund.CashBalance,
            cashPercentOfCeiling,
            belowAlertThreshold,
            awaitingReplenishment,
            new PettyCashDashboardInFlightDto(fund.InFlightAmount, fund.InFlightCount, olderThan5DaysCount),
            new PettyCashDashboardReturnedDto(returnedItems.Count, returnedOldestAgeDays),
            new PettyCashDashboardBalanceCheckDto(
                fund.Ceiling, fund.CashBalance, awaitingReplenishment.Amount, fund.InFlightAmount, paidSummary.Amount, balanced),
            todayActions,
            alerts);
    }
}
