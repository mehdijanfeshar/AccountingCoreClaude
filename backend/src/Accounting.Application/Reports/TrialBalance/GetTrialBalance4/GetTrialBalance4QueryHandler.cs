using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Reports.TrialBalance.GetTrialBalance4;

/// <summary>
/// Delegates to <see cref="ITrialBalanceReadRepository.GetAggregatesAsync"/> and projects each
/// <see cref="TrialBalanceAggregateRow"/> into a <see cref="TrialBalance4RowDto"/>. Never touches
/// <see cref="IUnitOfWork"/> — there is nothing to persist. Passes
/// <see cref="GetTrialBalance4Query.VahedCode"/> through at face value — by the time this handler
/// runs, <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's own
/// unit code.
/// </summary>
public sealed class GetTrialBalance4QueryHandler
    : IRequestHandler<GetTrialBalance4Query, IReadOnlyList<TrialBalance4RowDto>>
{
    private readonly ITrialBalanceReadRepository _readRepository;

    public GetTrialBalance4QueryHandler(ITrialBalanceReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<IReadOnlyList<TrialBalance4RowDto>> Handle(
        GetTrialBalance4Query request,
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
            .Select(r => new TrialBalance4RowDto(
                r.Code,
                r.Description,
                r.PeriodDebtor,
                r.PeriodCreditor,
                Math.Max(r.TotalDebtor - r.TotalCreditor, 0m),
                Math.Max(r.TotalCreditor - r.TotalDebtor, 0m)))
            .ToList();
    }
}
