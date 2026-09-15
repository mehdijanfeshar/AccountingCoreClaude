using MediatR;

namespace Accounting.Application.IdentityGroups.Queries.GetIdentityGroupById;

/// <summary>
/// Returns a single <c>TB_IDENTITYGROUP</c> row projected to <see cref="IdentityGroupDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="IdentityGroupDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetIdentityGroupByIdQuery(Guid Id) : IRequest<IdentityGroupDto?>;
