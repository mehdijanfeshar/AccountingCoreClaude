using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Queries.GetAccountCodeInterfaceById;

/// <summary>
/// Returns a single <c>TB_ACCOUNTCODE_INTERFACE</c> row projected to
/// <see cref="AccountCodeInterfaceDto"/>, or <see langword="null"/> if no row with the given
/// <see cref="Id"/> exists. Unlike the list query, no logical-delete filter is applied.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetAccountCodeInterfaceByIdQuery(Guid Id) : IRequest<AccountCodeInterfaceDto?>;
