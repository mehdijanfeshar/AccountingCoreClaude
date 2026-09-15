using MediatR;

namespace Accounting.Application.BillLogs.Queries.GetBillLogById;

/// <summary>
/// Returns a single <c>TB_BILL_LOG</c> row projected to <see cref="BillLogDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. No logical-delete
/// filter is applied.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetBillLogByIdQuery(Guid Id) : IRequest<BillLogDto?>;
