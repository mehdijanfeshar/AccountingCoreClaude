using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Vouchers.Commands.Common;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherDetail;

/// <summary>
/// Creates a new <c>TB_VOUCHERSDETAIL</c> row (Legacy voucher detail line) as a standalone
/// write against an ALREADY-EXISTING voucher head — e.g. adding a line to a voucher created
/// earlier. For creating a head together with its opening lines in one call, see
/// <see cref="Accounting.Application.Vouchers.Commands.CreateVoucherHead.CreateVoucherHeadCommand.InitialDetails"/>
/// instead. Carries primitive fields only — the handler is responsible for constructing the
/// Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// <b>VahedCode is now server-assigned (2026-09 IDOR closure), Year is not — this asymmetry is
/// documented, not guessed.</b> <see cref="VahedCode"/> below implements
/// <see cref="IVahedScopedCommand"/>: <c>VahedScopeBehavior</c> unconditionally overwrites it with
/// the authenticated caller's own unit code before this command reaches its handler, exactly like
/// <c>CreateVoucherHeadCommand.VahedCode</c>. <c>Year</c> is still plain caller input with no such
/// enforcement. Neither is cross-checked against the parent head's own <c>VAHEDCODE</c>/<c>YEAR</c>
/// — a caller whose own unit differs from the head's <c>VAHEDCODE</c> can still attach a line (with
/// their own <c>VahedCode</c>) to that head, because <c>CreateVoucherDetailCommandHandler</c>'s
/// parent-head lookup is itself unscoped (<c>GetById</c>-shaped access is explicitly out of scope
/// for this IDOR pass — see the project-owner decision recorded in the batch that introduced this
/// enforcement). Whether a standalone create should instead derive/validate <c>Year</c> from the
/// parent head, and whether the parent-head lookup itself should be unit-scoped, remain explicit
/// open decisions left to <c>team-lead</c>/the project owner — neither was resolved unilaterally
/// here.
/// </summary>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column — FK to the existing <c>TB_VOUCHERSHEAD</c> this line belongs to.</param>
/// <param name="AccountId">ACCOUNT_ID column — optional FK to <c>TB_ACCOUNTCODE</c>.</param>
/// <param name="ReceiptId">RECEIP_ID column — optional FK to <c>TB_RECEIP</c>.</param>
/// <param name="CheckId">CHECK_ID column.</param>
/// <param name="LowLevelCodeId">LOWLEVELCODE_ID column.</param>
/// <param name="EtebarId">ETEBAR_ID column.</param>
/// <param name="Description">DESCRIPTION column — شرح ردیف (max 200 chars).</param>
/// <param name="Radif">RADIF column — ردیف نمایش سطر.</param>
/// <param name="Debtor">DEBTOR column — مبلغ بدهکار.</param>
/// <param name="Creditor">CREDITOR column — مبلغ بستانکار.</param>
/// <param name="Year">YEAR column (max 4 chars). See the asymmetry note above — unlike
/// <see cref="VahedCode"/>, this is still plain caller input.</param>
/// <param name="TafsiliLinks">
/// Optional تفصیلی assignments (<c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> rows) to create together
/// with this detail line, in the SAME
/// <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/> call — so a
/// line can never be observed without the تفصیلی it was created with. <see langword="null"/> or
/// an empty list behaves byte-identically to a plain line-only create, keeping this strictly
/// non-breaking for every existing caller (the parameter is last and defaulted, mirroring
/// <c>CreateVoucherHeadCommand.InitialDetails</c>).
///
/// This is the write half of a table that until now could only be <i>deleted</i> (by the phase-9/10
/// cascades) — see <see cref="VoucherDetailTafsiliLinkInput"/> for why nesting it here is NOT
/// independent CRUD for an embedded table.
///
/// Duplicate <c>(TafsiliId, LevelId)</c> pairs within the supplied list are collapsed to a single
/// row by the handler rather than rejected: the same تفصیلی assigned twice to one line is one
/// assignment, and the Legacy table has no UNIQUE constraint to lean on
/// (<c>PK_VOUCHERDETAILLINKTAF</c> covers only <c>ID</c>), so silently writing two identical rows
/// would leave unremovable duplicate data.
/// </param>
public sealed record CreateVoucherDetailCommand(
    Guid VoucherHeadId,
    Guid? AccountId,
    Guid? ReceiptId,
    Guid? CheckId,
    Guid? LowLevelCodeId,
    Guid? EtebarId,
    string? Description,
    int? Radif,
    decimal? Debtor,
    decimal? Creditor,
    string? Year,
    IReadOnlyList<VoucherDetailTafsiliLinkInput>? TafsiliLinks = null) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (max 4 chars) — organizational unit for this line AND, per
    /// <see cref="TafsiliLinks"/> above, for every تفصیلی assignment created together with it.
    /// Never bound from the request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both
    /// model binding and the Swagger schema — and never trusted even if a caller manages to set
    /// it: <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated
    /// caller's own unit code before the request reaches <c>CreateVoucherDetailCommandHandler</c>.
    /// See <see cref="IVahedScopedCommand"/> for the full mechanism, and the asymmetry note above
    /// for what this does NOT close (the parent-head lookup itself, and <c>Year</c>).
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
