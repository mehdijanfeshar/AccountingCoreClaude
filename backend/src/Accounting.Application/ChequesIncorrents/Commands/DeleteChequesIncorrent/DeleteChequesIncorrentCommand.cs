using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.ChequesIncorrents.Commands.DeleteChequesIncorrent;

/// <summary>
/// Soft-deletes a <c>TB_CHEQUES_INCORRENT</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
/// </summary>
/// <param name="Id">The <c>TB_CHEQUES_INCORRENT.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteChequesIncorrentCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Caller's own organizational unit, server-assigned by <c>VahedScopeBehavior</c> — never
    /// client input. Used to refuse a row belonging to another unit; see
    /// <c>VahedOwnership</c> and IDOR risk #1.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
