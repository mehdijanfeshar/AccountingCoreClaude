using MediatR;

namespace Accounting.Application.BankAccounts.Queries.GetBankAccountById;

/// <summary>
/// Returns a single <c>TB_ACCOUNT</c> row projected to <see cref="BankAccountDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="BankAccountDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetBankAccountByIdQuery(Guid Id) : IRequest<BankAccountDto?>;
