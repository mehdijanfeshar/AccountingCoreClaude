using Accounting.Application.UnitAccess.Queries;
using MediatR;

namespace Accounting.Application.UnitAccess.Queries.GetAccessibleUnits;

/// <summary>
/// Every organizational unit the authenticated caller may act as — their own unit plus its
/// descendant subtree, or every unit when they are headquarters.
///
/// <para>
/// <b>Takes no parameters on purpose.</b> The caller's own unit comes from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser.VahedCode"/> (the token's org
/// claim) inside the handler, never from the request. A parameter here would let a caller ask
/// "what can <i>that</i> unit see", which is the enumeration hole this query exists to avoid.
/// It deliberately does NOT implement <c>IVahedScopedQuery</c> either — that interface exists to
/// have <c>VahedScopeBehavior</c> stamp a scope onto a request, and there is no field to stamp.
/// </para>
/// </summary>
public sealed record GetAccessibleUnitsQuery : IRequest<IReadOnlyList<AccessibleUnitDto>>;
