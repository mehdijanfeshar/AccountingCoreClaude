using Accounting.Application.IdentityHeads.Queries;
using MediatR;

namespace Accounting.Application.IdentityHeads.Queries.GetIdentityHeadById;

/// <summary>
/// Returns one شناسنامه with its fixed values, or <see langword="null"/> when no such row exists.
///
/// ⚠️ Deliberately NOT <c>IVahedScopedQuery</c> — consistent with every other GetById in this
/// project. Knowing an id is enough to read the row, including one belonging to another unit.
/// That is the still-open half of risk #1 and a conscious project-owner decision, not an
/// oversight here.
/// </summary>
public sealed record GetIdentityHeadByIdQuery(Guid Id) : IRequest<IdentityHeadDto?>;
