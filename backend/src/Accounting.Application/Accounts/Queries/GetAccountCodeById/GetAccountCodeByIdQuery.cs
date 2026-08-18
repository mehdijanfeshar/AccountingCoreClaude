using MediatR;

namespace Accounting.Application.Accounts.Queries.GetAccountCodeById;

/// <summary>
/// Returns a single <c>TB_ACCOUNTCODE</c> row projected to <see cref="AccountCodeDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="AccountCodeDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetAccountCodeByIdQuery(Guid Id) : IRequest<AccountCodeDto?>;
