using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.TmpVoucherHeads.Commands.CreateTmpVoucherHead;

/// <summary>
/// Creates a new <c>TB_TMP_VOUCHERHEAD</c> row (Legacy <em>temporary</em> voucher header —
/// سند موقت). Carries primitive fields only — the handler is responsible for constructing the
/// Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// ⚠️ <b>HEAD ONLY.</b> <c>TB_TMP_VOUCHERHEAD</c>'s child <c>TB_TMP_VOUCHERSDETAIL</c> is
/// explicitly out of scope — the aggregate boundary for this Head/Detail pair has not been
/// decided (see <c>docs/open-decisions.md</c>).
///
/// ⚠️⚠️ <b>This Head-only shape is a known, deliberate divergence from the reference project,
/// and it is a real functional gap — not just a scoping note.</b> In
/// <c>D:\CentralAccount</c>, <c>TmpVoucherDetail</c> is <b>encapsulated</b>: there is no
/// standalone detail command folder at all, and <c>AddTmpVoucherHeadCommandHandler.cs:32-58</c>
/// builds the whole <c>head + details</c> object graph in memory and persists it with a single
/// <c>AddAsync</c> (see <c>docs/centralaccount-business-reference.md</c> §5). That is the
/// <em>opposite</em> signal from <c>PayReciveDetail</c>, which the same project models as an
/// independent root. The practical consequence here: <b>this command can only ever create an
/// empty temporary voucher</b> — there is currently no way, in one call or several, to attach
/// lines to it. Turning that into the phase-10 "hybrid" shape (composite create) requires an
/// explicit aggregate-boundary decision from the project owner; it was not assumed.
///
/// <c>VoucherHeadId</c> is backed by a real, optional FK (<c>FK_TMP_VOCHERHEAD</c> to
/// <c>TB_VOUCHERSHEAD</c>, <c>OnDelete(SetNull)</c>), mapped centrally to 400 by
/// <c>UnitOfWork.SaveChangesAsync</c> on violation. It is the table's <em>only</em> FK.
///
/// ⚠️⚠️ <b><c>SourceId</c> (<c>SOURCEID</c>) has NO FK at all in Legacy</b> — an invalid value is
/// written <em>silently</em>; the central ORA-02291 → 400 mapping does not help because there is
/// no constraint to violate. Same gap pattern as
/// <c>TB_VOUCHERDETAIL_LINK_TAFSILI.TAFSILI_ID</c> (phase 11), <c>TB_VAHED_INFO.CITY_ID</c>
/// (phase 14) and <c>TB_ELAMHEAD.WORKSHOP_ID</c> (phase 15). No pre-check is added (that would
/// be inventing a business rule, and would be race-prone besides).
///
/// <c>TB_TMP_VOUCHERHEAD</c> has <b>no UNIQUE constraint at all</b>, so 409 is never declared —
/// nothing here is guaranteed distinct, including <c>SourceId</c>, which means the same source
/// document can be staged twice without complaint.
///
/// Every single column that reaches this command through its positional parameters is nullable,
/// so this command has no <c>NotEmpty</c> rules on those — only <c>MaximumLength</c>. Contrast
/// <c>CreatePayReciveHeadCommand</c>, whose table has five NOT NULL columns. <c>VahedCode</c> is
/// the sole exception: even though <c>VAHEDCODE</c> is nullable at the Legacy schema level, it is
/// no longer client-supplied at all (see below), so its validator rule is <c>NotEmpty</c>.
/// </summary>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column (optional) — the real voucher this temporary one was promoted into, if any (<c>FK_TMP_VOCHERHEAD</c>).</param>
/// <param name="DateDoc">DATE_DOC column (optional, max 8 chars — Legacy string-encoded document date, not a real <see cref="DateTime"/>).</param>
/// <param name="HeadDesc">HEAD_DESC column (optional, max 250 chars — free-text document description).</param>
/// <param name="Year">YEAR column (optional, max 4 chars, fixed-length — fiscal year).</param>
/// <param name="SysType">SYS_TYPE column (optional, <b>max 1 char</b>) — a single-character source-system discriminator. It is a <see cref="string"/>, not a numeric column, so the <c>bool?</c>-should-be-enum pattern does not apply; but no allowed-value set is known, so no value rule is invented beyond the length limit.</param>
/// <param name="SourceId">SOURCEID column (optional <see cref="Guid"/>) — ⚠️ NO FK at all; invalid values are written silently.</param>
public sealed record CreateTmpVoucherHeadCommand(
    Guid? VoucherHeadId,
    string? DateDoc,
    string? HeadDesc,
    string? Year,
    string? SysType,
    Guid? SourceId) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (<c>VAHEDCODE</c> column — nullable at the Legacy schema level,
    /// but always populated with a real value here). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>CreateTmpVoucherHeadCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
