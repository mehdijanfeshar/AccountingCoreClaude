using MediatR;

namespace Accounting.Application.VahedTypes.Queries.GetVahedTypes;

/// <summary>
/// Returns every <c>TB_VAHED_TYPE</c> row, ordered by <c>PARENTTYPECODE</c> then <c>TYPECODE</c>.
///
/// <b>Deliberately unpaged.</b> The table is a fixed lookup — 17 rows on live data, and it grows
/// only when the organisation itself gains a new kind of unit. Its single consumer is a
/// three-level checkbox tree (ایران → بخش → نوع واحد), which cannot render a page of a tree; the
/// same reasoning as the unpaged «سطوح تفصیلی» screen in phase 36. If this table ever reaches a
/// size where that stops being true, the tree UI has a bigger problem than the query does.
///
/// It takes no parameters at all, which is also a security property rather than brevity: there is
/// no unit-scoped subject here to get wrong. <c>TB_VAHED_TYPE</c> has no <c>VAHEDCODE</c> column —
/// it is a global lookup, and is listed as such in <c>VahedScopeConventionTests</c>.
/// </summary>
public sealed record GetVahedTypesQuery : IRequest<IReadOnlyList<VahedTypeDto>>;
