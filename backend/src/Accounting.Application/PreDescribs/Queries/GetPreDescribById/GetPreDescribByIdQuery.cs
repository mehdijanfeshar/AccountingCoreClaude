using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PreDescribs.Queries.GetPreDescribById;

/// <summary>
/// Returns a single <c>TB_PREDESCRIB</c> row projected to <see cref="PreDescribDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists.
/// </summary>
/// <param name="Id">ID column to look up.</param>
public sealed record GetPreDescribByIdQuery(Guid Id) : IRequest<PreDescribDto?>, IVahedScopedQuery
{
    /// <summary>
    /// Caller's own organizational unit, server-assigned by <c>VahedScopeBehavior</c> — never
    /// client input. Used to refuse a row belonging to another unit; see
    /// <c>VahedOwnership</c> and IDOR risk #1.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
