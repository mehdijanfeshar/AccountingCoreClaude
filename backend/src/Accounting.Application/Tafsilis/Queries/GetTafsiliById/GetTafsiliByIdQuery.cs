using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Tafsilis.Queries.GetTafsiliById;

/// <summary>
/// Returns a single <c>TB_TAFSILI</c> row projected to <see cref="TafsiliDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>.
///
/// <para>
/// <b>The unit filter, however, now applies.</b> This doc used to say the row comes back
/// regardless of <c>VAHEDCODE</c> and that this action never returns 403, citing
/// <c>GetExpenseByIdQuery</c> as precedent — that was a description of the open half of IDOR
/// risk #1, and both queries have since been scoped. Asking for another unit's تفصیلی answers
/// 403. Globally-shared rows (blank <c>VAHEDCODE</c>, what the reference project writes for
/// <c>Owner = Global</c>) stay visible to every unit.
/// </para>
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetTafsiliByIdQuery(Guid Id) : IRequest<TafsiliDto?>, IVahedScopedQuery
{
    /// <summary>
    /// Caller's own organizational unit, server-assigned by <c>VahedScopeBehavior</c> — never
    /// client input.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
