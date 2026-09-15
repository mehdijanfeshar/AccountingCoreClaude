using MediatR;

namespace Accounting.Application.Receipts.Queries.GetReceiptById;

/// <summary>
/// Returns a single <c>TB_RECEIP</c> row projected to <see cref="ReceiptDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="ReceiptDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetReceiptByIdQuery(Guid Id) : IRequest<ReceiptDto?>;
