using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Reports.TrialBalance.GetTrialBalance8;

/// <summary>
/// Delegates to <see cref="ITrialBalanceReadRepository.GetAggregatesAsync"/> and projects each
/// <see cref="TrialBalanceAggregateRow"/> into a <see cref="TrialBalance8RowDto"/>. Never touches
/// <see cref="IUnitOfWork"/> — there is nothing to persist.
///
/// <see cref="TrialBalance8RowDto.TotDebtor"/>/<see cref="TrialBalance8RowDto.TotCreditor"/> are
/// mapped straight from <see cref="TrialBalanceAggregateRow.TotalDebtor"/>/
/// <see cref="TrialBalanceAggregateRow.TotalCreditor"/> (raw cumulative turnover, intentionally
/// NOT netted — a cumulative turnover column, unlike a balance column, can legitimately be
/// non-zero on both sides). ⚠️ <see cref="TrialBalance8RowDto.FirstDebtor"/>/
/// <see cref="TrialBalance8RowDto.FirstCreditor"/>, by contrast, ARE netted (one-sided
/// "مانده اول دوره" balance — project owner's explicit decision, see
/// <c>docs/open-decisions.md</c>), so the raw arithmetic identity
/// <c>TotDebtor == FirstDebtor + Debtor</c> does **not** generally hold once one side of
/// <c>FirstDebtor</c>/<c>FirstCreditor</c> has been zeroed by <c>Math.Max</c> — it only held back
/// when <c>FirstDebtor</c>/<c>FirstCreditor</c> were still raw. Do not reintroduce a test asserting
/// that identity.
/// </summary>
public sealed class GetTrialBalance8QueryHandler
    : IRequestHandler<GetTrialBalance8Query, IReadOnlyList<TrialBalance8RowDto>>
{
    private readonly ITrialBalanceReadRepository _readRepository;

    public GetTrialBalance8QueryHandler(ITrialBalanceReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<IReadOnlyList<TrialBalance8RowDto>> Handle(
        GetTrialBalance8Query request,
        CancellationToken cancellationToken)
    {
        var rows = await _readRepository.GetAggregatesAsync(
            request.Level,
            request.Year,
            request.FromDate,
            request.ToDate,
            request.VahedCode,
            request.DocLife,
            cancellationToken);

        return rows
            .Select(r => new TrialBalance8RowDto(
                r.Code,
                r.Description,
                r.PeriodDebtor,
                r.PeriodCreditor,
                Math.Max(r.TotalDebtor - r.TotalCreditor, 0m),
                Math.Max(r.TotalCreditor - r.TotalDebtor, 0m),
                Math.Max(r.OpeningDebtor - r.OpeningCreditor, 0m),
                Math.Max(r.OpeningCreditor - r.OpeningDebtor, 0m),
                r.TotalDebtor,
                r.TotalCreditor))
            .ToList();
    }
}
