namespace Accounting.Application.Vouchers.Queries;

/// <summary>
/// Read-side projection of <c>TB_VOUCHERSHEAD</c>. Used by both <c>GetVoucherHeads</c> (list)
/// and <c>GetVoucherHeadById</c> — the Domain entity never crosses the Application boundary.
///
/// Deliberately excludes <c>ATTACHFILE</c> (the BLOB attachment column): it must never be
/// loaded for a list/detail read. A dedicated attachment-download use case can add it later
/// without touching this DTO.
/// </summary>
/// <param name="Id">ID column — آي دي سند.</param>
/// <param name="DocNum">DOC_NUM column — شماره واقعی سند.</param>
/// <param name="DateDoc">DATE_DOC column — تاریخ سند (legacy string date format).</param>
/// <param name="DocLife">DOCLIFE column — وضعیت سند.</param>
/// <param name="HeadDesc">HEAD_DESC column — شرح سند.</param>
/// <param name="Apendix">APENDIX column — پیوست.</param>
/// <param name="SystemTypeId">Optional FK to <c>TB_SYSTYPE</c> — نوع سیستم.</param>
/// <param name="FlagState">FLAG_STATE column — سند آیا اختتامیه می‌باشد.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="VahedCode">VAHEDCODE column — کد واحد.</param>
/// <param name="Year">YEAR column.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
/// <param name="AttachFileName">ATTACHFILE_NAME column — file name of the attachment, NOT the BLOB itself.</param>
/// <param name="AtfNum">ATF_NUM column.</param>
/// <param name="IsAutomatic">ISAUTOMATIC column — 0 دستی و 1 مکانیزه.</param>
/// <param name="SndVahedCode">SNDVAHEDCODE column — واحد گیرنده.</param>
/// <param name="ParentHeadId">Optional self-referencing parent voucher head.</param>
/// <param name="GlobalNumber">GLOBALNUMBER column (legacy-generated sequence-like field).</param>
public sealed record VoucherHeadDto(
    Guid Id,
    string? DocNum,
    string? DateDoc,
    bool? DocLife,
    string? HeadDesc,
    string? Apendix,
    Guid? SystemTypeId,
    decimal? FlagState,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    string? VahedCode,
    string? Year,
    bool? IsDeleted,
    string? AttachFileName,
    string? AtfNum,
    bool? IsAutomatic,
    string? SndVahedCode,
    Guid? ParentHeadId,
    string? GlobalNumber);
