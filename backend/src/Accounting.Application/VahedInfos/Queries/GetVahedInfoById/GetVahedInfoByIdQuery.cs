using MediatR;

namespace Accounting.Application.VahedInfos.Queries.GetVahedInfoById;

/// <summary>
/// Returns a single <c>TB_VAHED_INFO</c> row projected to <see cref="VahedInfoDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetVahedInfoByIdQuery(Guid Id) : IRequest<VahedInfoDto?>;
