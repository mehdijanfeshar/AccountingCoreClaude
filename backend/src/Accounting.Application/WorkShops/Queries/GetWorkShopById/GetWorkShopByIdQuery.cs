using MediatR;

namespace Accounting.Application.WorkShops.Queries.GetWorkShopById;

/// <summary>
/// Returns a single <c>TB_WORKSHOP</c> row projected to <see cref="WorkShopDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="WorkShopDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetWorkShopByIdQuery(Guid Id) : IRequest<WorkShopDto?>;
