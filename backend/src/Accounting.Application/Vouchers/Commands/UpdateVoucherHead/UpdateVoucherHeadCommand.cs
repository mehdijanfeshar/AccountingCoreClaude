using MediatR;

namespace Accounting.Application.Vouchers.Commands.UpdateVoucherHead;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_VOUCHERSHEAD</c> row (PUT
/// semantics, not PATCH). This is deliberate: almost every Legacy column here is nullable
/// (<c>bool?</c>, <c>Guid?</c>, <c>string?</c>, <c>decimal?</c>), so a partial-update model
/// cannot distinguish "field omitted by the caller" from "field explicitly set to null"
/// without wrapping every field in an <c>Optional&lt;T&gt;</c>-style marker — which both
/// breaks the "commands carry only primitives" rule and adds machinery disproportionate to
/// the need. PUT also keeps this command symmetric with <c>CreateVoucherHeadCommand</c>.
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c>, <c>ISDELETED</c> and
/// <c>GLOBALNUMBER</c> (mapped <c>ValueGeneratedOnAdd</c> in <c>LegacyDbContext</c> — a
/// legacy-generated sequence-like field, never caller input): identity and creation audit are
/// immutable after insert, and <c>ISDELETED</c> is owned exclusively by
/// <c>DeleteVoucherHeadCommand</c> — allowing it here would turn Update into a back door for
/// delete/undelete. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are likewise absent because the
/// handler sources them from <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/>
/// and the server clock, never from client input. <c>ATTACHFILE</c> (BLOB attachment upload)
/// stays out of scope here too, matching <c>CreateVoucherHeadCommand</c>.
/// </summary>
/// <param name="Id">The <c>TB_VOUCHERSHEAD.ID</c> to update (bound from the route, never the body).</param>
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
public sealed record UpdateVoucherHeadCommand(
    Guid Id,
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
    string? AtfNum) : IRequest;
