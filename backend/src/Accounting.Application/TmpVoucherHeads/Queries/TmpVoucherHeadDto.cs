namespace Accounting.Application.TmpVoucherHeads.Queries;

/// <summary>
/// Read-side projection of <c>TB_TMP_VOUCHERHEAD</c>. Used by both <c>GetTmpVoucherHeads</c>
/// (list) and <c>GetTmpVoucherHeadById</c> — the Domain entity never crosses the Application
/// boundary.
///
/// ⚠️ HEAD ONLY — carries no <c>TB_TMP_VOUCHERSDETAIL</c> data (out of scope; see
/// <c>Accounting.Api.Controllers.TmpVoucherHeadsController</c> XML doc). Since the write side
/// cannot create detail rows either, a temporary voucher is currently opaque from the API's
/// point of view: you can see that one was staged, but not what it contains.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column — the real voucher this temporary one was promoted into, if any.</param>
/// <param name="DateDoc">DATE_DOC column — Legacy string-encoded document date.</param>
/// <param name="HeadDesc">HEAD_DESC column — free-text document description.</param>
/// <param name="VahedCode">VAHEDCODE column — organizational unit code.</param>
/// <param name="Year">YEAR column — fiscal year.</param>
/// <param name="SysType">SYS_TYPE column — single-character source-system discriminator.</param>
/// <param name="SourceId">SOURCEID column — ⚠️ NO FK at all, so this may point at nothing.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
public sealed record TmpVoucherHeadDto(
    Guid Id,
    Guid? VoucherHeadId,
    string? DateDoc,
    string? HeadDesc,
    string? VahedCode,
    string? Year,
    string? SysType,
    Guid? SourceId,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted);
