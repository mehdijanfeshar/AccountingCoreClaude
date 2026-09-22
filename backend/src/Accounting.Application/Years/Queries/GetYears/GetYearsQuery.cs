using Accounting.Application.Years.Queries;
using MediatR;

namespace Accounting.Application.Years.Queries.GetYears;

/// <summary>
/// Every financial year in <c>TB_YEAR</c>, newest first.
///
/// Not paginated and not scoped: <c>TB_YEAR</c> is a handful of rows and has no
/// <c>VAHEDCODE</c> column, so financial years are global to the installation — the same list is
/// correct for every caller.
/// </summary>
public sealed record GetYearsQuery : IRequest<IReadOnlyList<YearDto>>;
