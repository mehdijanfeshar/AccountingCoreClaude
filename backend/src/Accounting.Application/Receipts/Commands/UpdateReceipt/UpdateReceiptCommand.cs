using MediatR;

namespace Accounting.Application.Receipts.Commands.UpdateReceipt;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_RECEIP</c> row (PUT semantics, not
/// PATCH) — mirrors <c>UpdateWorkShopCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c>
/// are likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock.
///
/// Field-by-field documentation is intentionally not repeated here — every field (including
/// <c>Id</c>, bound from the route and never the body) has the exact same column/type/length
/// meaning as the identically-named parameter on
/// <see cref="Accounting.Application.Receipts.Commands.CreateReceipt.CreateReceiptCommand"/>; see
/// that command's XML doc for the unverified-enum flag on <c>ReceiptKind</c>.
/// </summary>
public sealed record UpdateReceiptCommand(
    Guid Id,
    bool ReceiptKind,
    string ReceiptDate,
    string ReceiptNo,
    string? DateRsid,
    string VahedCode,
    string Year) : IRequest;
