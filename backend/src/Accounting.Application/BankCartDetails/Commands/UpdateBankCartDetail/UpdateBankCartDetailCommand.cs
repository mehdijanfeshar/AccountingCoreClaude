using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.BankCartDetails.Commands.UpdateBankCartDetail;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_BANKCARTDETAIL</c> row (PUT semantics,
/// not PATCH) — mirrors <c>UpdateWorkShopCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c>
/// are likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock.
///
/// Field-by-field documentation is intentionally not repeated here — every field (including
/// <c>Id</c>, bound from the route and never the body) has the exact same column/type/length
/// meaning as the identically-named parameter on
/// <see cref="Accounting.Application.BankCartDetails.Commands.CreateBankCartDetail.CreateBankCartDetailCommand"/>;
/// see that command's XML doc for the FK/UNIQUE/enum/balance flags, and for
/// <see cref="VahedCode"/> below, for why it is non-nullable here despite the Legacy column being
/// nullable.
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update (i.e. "does this <c>Id</c> already belong to
/// the caller's unit?") is explicitly out of scope for this pass, by project-owner decision. The
/// IDOR risk on direct-by-id access therefore remains open for Update; only the "what unit does
/// this row end up in" half of the problem is closed here.
/// </summary>
public sealed record UpdateBankCartDetailCommand(
    Guid Id,
    Guid? ReceipId,
    Guid? CheckId,
    Guid? BankId,
    Guid? BranchId,
    string? AccountNumber,
    string? Month,
    string? Cheqno,
    string? RecivDate,
    bool? CheckReceiptType,
    decimal? Debtor,
    decimal? Creditor,
    string? Year,
    Guid? CheckIncorrentId) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (max 4 chars). See
    /// <c>CreateBankCartDetailCommand.VahedCode</c> XML doc for why this is non-nullable
    /// <see cref="string"/> despite <c>TB_BANKCARTDETAIL.VAHEDCODE</c> being nullable in Legacy.
    /// Never bound from the request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both
    /// model binding and the Swagger schema — and never trusted even if a caller manages to set
    /// it: <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated
    /// caller's own unit code before the request reaches
    /// <c>UpdateBankCartDetailCommandHandler</c>. See <see cref="IVahedScopedCommand"/> for the
    /// full mechanism, and the scope note above for what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
