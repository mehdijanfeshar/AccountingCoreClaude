using MediatR;

namespace Accounting.Application.SysTypes.Queries.GetSysTypes;

/// <summary>
/// Returns every <c>TB_SYSTYPE</c> row (نوع سند), ordered by <c>SYS_COD</c>.
///
/// Deliberately NOT paged and NOT unit-scoped: this is a static reference table of a handful of
/// rows with no <c>VAHEDCODE</c> column, existing only to populate the نوع سند picker on the
/// voucher list. It therefore does not implement <c>IVahedScopedQuery</c> — there is nothing to
/// scope, and pretending otherwise would imply a per-unit filter this table cannot support.
/// </summary>
public sealed record GetSysTypesQuery : IRequest<IReadOnlyList<SysTypeDto>>;
