using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodeById;

/// <summary>
/// Returns a single <c>TB_ATTRIBFORACCOUNTCODE</c> row projected to
/// <see cref="AttribForAccountCodeDto"/>, or <see langword="null"/> if no row with the given
/// <see cref="Id"/> exists. Unlike the list query, no logical-delete filter is applied — the row
/// is returned regardless of <c>ISDELETED</c>, and <see cref="AttribForAccountCodeDto.IsDeleted"/>
/// lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetAttribForAccountCodeByIdQuery(Guid Id) : IRequest<AttribForAccountCodeDto?>;
