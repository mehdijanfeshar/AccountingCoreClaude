using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.WorkShops.Commands.UpdateWorkShop;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_WORKSHOP</c> row (PUT semantics, not
/// PATCH) — the same replace-vs-patch rationale as <c>UpdateAccountCodeCommand</c> applies here.
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>:
/// identity and creation audit are immutable after insert, and <c>ISDELETED</c> is owned
/// exclusively by <c>DeleteWorkShopCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are
/// likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update (i.e. "does this <c>Id</c> already belong to
/// the caller's unit?") is explicitly out of scope for this pass, by project-owner decision. The
/// IDOR risk on direct-by-id access therefore remains open for Update; only the "what unit does
/// this row end up in" half of the problem is closed here.
/// </summary>
/// <param name="Id">The <c>TB_WORKSHOP.ID</c> to update (bound from the route, never the body).</param>
/// <param name="AccountCodeId">Required link to <c>TB_ACCOUNTCODE</c> (<c>FK_WORK_ACCOUNTCODE</c>).</param>
/// <param name="BranchId">Optional link to <c>TB_VAHED_INFO</c> (<c>FK_WORK_VAHEDINFO</c>).</param>
/// <param name="WorkShopName">Workshop name (required, max 100 chars).</param>
/// <param name="WorkShopCode">Workshop code (required, max 10 chars; part of <c>UK_WORKSHOP</c>).</param>
/// <param name="IsActive">ISACTIVE column — non-nullable flag, part of <c>UK_WORKSHOP</c>; see <c>CreateWorkShopCommand</c> XML doc for the unverified-enum note.</param>
/// <param name="CheckFile">Optional Oracle BLOB (JSON base64); no size limit enforced here.</param>
public sealed record UpdateWorkShopCommand(
    Guid Id,
    Guid AccountCodeId,
    Guid? BranchId,
    string WorkShopName,
    string WorkShopCode,
    bool IsActive,
    byte[]? CheckFile) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (required, max 4 chars; part of <c>UK_WORKSHOP</c>). Never bound
    /// from the request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both model
    /// binding and the Swagger schema — and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated caller's
    /// own unit code before the request reaches <c>UpdateWorkShopCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism, and the scope note above for
    /// what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
