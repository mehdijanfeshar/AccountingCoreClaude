using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PayReciveHeads.Commands.DeletePayReciveHead;

/// <summary>
/// Soft-deletes a <c>TB_PAYRECIVHEAD</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
///
/// ⚠️ HEAD ONLY — does NOT cascade to <c>TB_PAYRECIVDETAIL</c>, and does NOT re-create the
/// reference project's «برای ردیف انتخابی سند صادر شده و قابل حذف نمی باشد» guard (which blocks
/// deleting a document that has already been turned into an accounting voucher). See
/// <c>Accounting.Api.Controllers.PayReciveHeadsController</c> XML doc.
/// </summary>
/// <param name="Id">The <c>TB_PAYRECIVHEAD.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeletePayReciveHeadCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Caller's own organizational unit, server-assigned by <c>VahedScopeBehavior</c> — never
    /// client input. Used to refuse a row belonging to another unit; see
    /// <c>VahedOwnership</c> and IDOR risk #1.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
