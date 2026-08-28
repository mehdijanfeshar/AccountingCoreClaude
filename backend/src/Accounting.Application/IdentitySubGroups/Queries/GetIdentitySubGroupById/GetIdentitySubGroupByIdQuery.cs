using MediatR;

namespace Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroupById;

/// <summary>
/// Returns a single <c>TB_IDENTITYSUBGRP</c> row projected to <see cref="IdentitySubGroupDto"/>,
/// or <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="IdentitySubGroupDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetIdentitySubGroupByIdQuery(Guid Id) : IRequest<IdentitySubGroupDto?>;
