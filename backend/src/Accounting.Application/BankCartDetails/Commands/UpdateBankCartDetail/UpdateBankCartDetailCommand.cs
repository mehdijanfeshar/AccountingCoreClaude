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
/// see that command's XML doc for the FK/UNIQUE/enum/balance flags.
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
    string? VahedCode,
    string? Year,
    Guid? CheckIncorrentId) : IRequest;
