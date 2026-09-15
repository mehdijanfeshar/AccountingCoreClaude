using MediatR;

namespace Accounting.Application.Tafsilis.Queries.GetTafsiliById;

/// <summary>
/// Returns a single <c>TB_TAFSILI</c> row projected to <see cref="TafsiliDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete or Vahed filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>/<c>VAHEDCODE</c>, matching <c>GetExpenseByIdQuery</c>/<c>GetTafsilGroupByIdQuery</c>
/// precedent (neither is <c>IVahedScopedQuery</c>, so this action never returns 403).
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetTafsiliByIdQuery(Guid Id) : IRequest<TafsiliDto?>;
