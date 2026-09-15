using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
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
/// <param name="Year">YEAR column (max 4 chars).</param>
/// <param name="IsAutomatic">ISAUTOMATIC column — 0 دستی و 1 مکانیزه.</param>
/// <param name="SndVahedCode">SNDVAHEDCODE column — واحد گیرنده (max 4 chars).</param>
/// <param name="ParentHeadId">Optional self-referencing parent voucher head.</param>
/// <param name="AttachFileName">ATTACHFILE_NAME column (max 100 chars).</param>
/// <param name="AtfNum">ATF_NUM column (max 15 chars, fixed length).</param>
/// <param name="InitialDetails">
/// Optional initial <c>TB_VOUCHERSDETAIL</c> lines to create together with this head, in the
/// SAME <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/>
/// call (composite create — option "الف" of the 2026-08-20 project-owner decision recorded in
/// <c>docs/tamin-core-entity-reference.md</c> بخش ۵: <c>TB_VOUCHERSDETAIL</c> is an independent
/// aggregate with its own standalone CRUD, but the very first write for a brand-new voucher must
/// still be able to persist header + lines atomically). <see langword="null"/> or an empty list
/// behaves byte-identically to a plain header-only create — no behavioural change for any
/// existing caller.
///
/// <b>Documented, deliberate exception to the phase-5 "Command carries only primitives" rule:</b>
/// that rule exists so a Command can never smuggle a Domain entity across the API/Application
/// boundary (a Domain entity carries EF Core change-tracking semantics, navigation properties,
/// and identity that a transport object must never have). <see cref="CreateVoucherHeadDetailInput"/>
/// is a nested <c>sealed record</c> of primitives only — not a Domain entity, not
/// <see cref="Accounting.Domain.Entity.TB_VOUCHERSDETAIL"/> — so nesting it here is compliant
/// with the rule's actual purpose, and this exception is recorded explicitly rather than
/// silently bent.
///
/// The response type of this command intentionally stays <c>Guid</c> (the head's <c>ID</c>
/// only) — deliberate, to keep this change strictly non-breaking for every existing caller of
/// <see cref="CreateVoucherHeadCommand"/>. The generated detail line IDs are not returned here;
/// they are discoverable via <c>GET /api/voucher-details?voucherHeadId={id}</c>
/// (<see cref="Accounting.Application.Vouchers.Queries.GetVoucherDetails.GetVoucherDetailsQuery"/>)
/// after this command completes.
/// </param>
public sealed record CreateVoucherHeadCommand(
    string DocNum,
    string DateDoc,
    bool? DocLife,
    string? HeadDesc,
    string? Apendix,
    Guid? SystemTypeId,
    decimal? FlagState,
    string Year,
    bool? IsAutomatic,
    string? SndVahedCode,
    Guid? ParentHeadId,
    string? AttachFileName,
    string? AtfNum,
    IReadOnlyList<CreateVoucherHeadDetailInput>? InitialDetails = null) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (max 4 chars) — organizational unit for this head AND, per the
    /// <see cref="InitialDetails"/> doc above, for every composite-created
    /// <c>TB_VOUCHERSDETAIL</c> line as well. Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>CreateVoucherHeadCommandHandler</c>. See <see cref="IVahedScopedCommand"/>
    /// for the full mechanism. This is the fix for IDOR risk #1 (CLAUDE.md) applied to the
    /// heaviest write path in the project — a caller can no longer create a voucher head (or its
    /// opening detail lines) in any unit but their own.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
