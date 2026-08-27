using MediatR;

namespace Accounting.Application.AccountExceptions.Queries.GetAccountExceptionById;

/// <summary>
/// Returns a single <c>TB_ACCOUNTEXCEPTION</c> row projected to <see cref="AccountExceptionDto"/>,
/// or <see langword="null"/> if no row with the given <see cref="Id"/> exists. No logical-delete
/// filter is applied.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetAccountExceptionByIdQuery(Guid Id) : IRequest<AccountExceptionDto?>;
