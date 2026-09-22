using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.IdentityHeads.Queries;
using MediatR;

namespace Accounting.Application.IdentityHeads.Queries.GetIdentityHeadById;

/// <summary>
/// Returns one شناسنامه with its fixed values, or <see langword="null"/> when no such row exists.
///
/// ✅ <c>IVahedScopedQuery</c> as of 2026-09-21. This doc previously recorded the opposite, and
/// accurately so at the time: knowing an id was enough to read another unit's row, the open half
/// of risk #1, left that way by a conscious decision rather than an oversight. The project owner
/// has since reversed that decision and every GetById in the project is scoped — asking for
/// another unit's شناسنامه now answers 403.
/// </summary>
public sealed record GetIdentityHeadByIdQuery(Guid Id) : IRequest<IdentityHeadDto?>, IVahedScopedQuery
{
    /// <summary>
    /// Caller's own organizational unit, server-assigned by <c>VahedScopeBehavior</c> — never
    /// client input.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
