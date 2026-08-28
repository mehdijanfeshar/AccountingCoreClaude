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
/// </summary>
/// <param name="Id">The <c>TB_WORKSHOP.ID</c> to update (bound from the route, never the body).</param>
/// <param name="AccountCodeId">Required link to <c>TB_ACCOUNTCODE</c> (<c>FK_WORK_ACCOUNTCODE</c>).</param>
/// <param name="BranchId">Optional link to <c>TB_VAHED_INFO</c> (<c>FK_WORK_VAHEDINFO</c>).</param>
/// <param name="WorkShopName">Workshop name (required, max 100 chars).</param>
/// <param name="WorkShopCode">Workshop code (required, max 10 chars; part of <c>UK_WORKSHOP</c>).</param>
/// <param name="VahedCode">Organizational unit code (required, max 4 chars; part of <c>UK_WORKSHOP</c>).</param>
/// <param name="IsActive">ISACTIVE column — non-nullable flag, part of <c>UK_WORKSHOP</c>; see <c>CreateWorkShopCommand</c> XML doc for the unverified-enum note.</param>
/// <param name="CheckFile">Optional Oracle BLOB (JSON base64); no size limit enforced here.</param>
public sealed record UpdateWorkShopCommand(
    Guid Id,
    Guid AccountCodeId,
    Guid? BranchId,
    string WorkShopName,
    string WorkShopCode,
    string VahedCode,
    bool IsActive,
    byte[]? CheckFile) : IRequest;
