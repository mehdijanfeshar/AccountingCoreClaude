using MediatR;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherHead;

/// <summary>
/// Creates a new <c>TB_VOUCHERSHEAD</c> row (Legacy voucher header). Carries primitive
/// fields only — the handler is responsible for constructing the Domain entity. Returns the
/// newly generated <see cref="Guid"/> ID.
///
/// Deliberately excludes <c>GLOBALNUMBER</c> (mapped <c>ValueGeneratedOnAdd</c> in
/// <c>LegacyDbContext</c> — a legacy-generated sequence-like field, not caller input) and
/// <c>ATTACHFILE</c> (BLOB attachment upload — out of scope for this first command; a
/// dedicated attachment use case can add it later without touching this one).
/// </summary>
/// <param name="DocNum">DOC_NUM column — شماره واقعی سند (max 6 chars). Combined with Year/VahedCode must be unique (<c>UK_VOUCHERHEAD_NUMBER</c>).</param>
/// <param name="DateDoc">DATE_DOC column — تاریخ سند (max 8 chars, legacy string date format).</param>
/// <param name="DocLife">DOCLIFE column — وضعیت سند.</param>
/// <param name="HeadDesc">HEAD_DESC column — شرح سند (max 250 chars).</param>
/// <param name="Apendix">APENDIX column — پیوست (max 800 chars).</param>
/// <param name="SystemTypeId">Optional FK to <c>TB_SYSTYPE</c> — نوع سیستم.</param>
/// <param name="FlagState">FLAG_STATE column — سند آیا اختتامیه می‌باشد.</param>
/// <param name="VahedCode">VAHEDCODE column — کد واحد (max 4 chars).</param>
/// <param name="Year">YEAR column (max 4 chars).</param>
/// <param name="IsAutomatic">ISAUTOMATIC column — 0 دستی و 1 مکانیزه.</param>
/// <param name="SndVahedCode">SNDVAHEDCODE column — واحد گیرنده (max 4 chars).</param>
/// <param name="ParentHeadId">Optional self-referencing parent voucher head.</param>
/// <param name="AttachFileName">ATTACHFILE_NAME column (max 100 chars).</param>
/// <param name="AtfNum">ATF_NUM column (max 15 chars, fixed length).</param>
/// <param name="AddUserId">Creating user identifier (max 10 chars). No ICurrentUser abstraction exists yet, so this is supplied by the caller.</param>
public sealed record CreateVoucherHeadCommand(
    string DocNum,
    string DateDoc,
    bool? DocLife,
    string? HeadDesc,
    string? Apendix,
    Guid? SystemTypeId,
    decimal? FlagState,
    string VahedCode,
    string Year,
    bool? IsAutomatic,
    string? SndVahedCode,
    Guid? ParentHeadId,
    string? AttachFileName,
    string? AtfNum,
    string? AddUserId) : IRequest<Guid>;
