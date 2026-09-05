using MediatR;

namespace Accounting.Application.ElamHeads.Commands.CreateElamHead;

/// <summary>
/// Creates a new <c>TB_ELAMHEAD</c> row (Legacy announcement/notice header — اعلاميه). Carries
/// primitive fields only — the handler is responsible for constructing the Domain entity.
/// Returns the newly generated <see cref="Guid"/> ID.
///
/// ⚠️ <b>HEAD ONLY.</b> <c>TB_ELAMHEAD</c>'s child <c>TB_ELAMDETAIL</c> is explicitly out of
/// scope — the aggregate boundary for this Head/Detail pair has not been decided (see
/// <c>docs/open-decisions.md</c>). This command follows the phase-5..8 <c>VoucherHead</c>
/// precedent exactly: standalone Head CRUD only, no composite create, no cascade. See
/// <c>Accounting.Api.Controllers.ElamHeadsController</c> XML doc for the explicit statement that
/// this is deliberate, not an oversight.
///
/// <c>VoucherHeadId</c> is backed by a real, optional FK (<c>FK_ELAM_VOUCHER</c> to
/// <c>TB_VOUCHERSHEAD</c>), mapped centrally to 400 by <c>UnitOfWork.SaveChangesAsync</c> on
/// violation. <c>(SerialNo, Code, VahedCode)</c> together are protected by the real UNIQUE
/// constraint <c>AK_AK_ELAMHEAD_ELAMHEAD</c>, mapped centrally to 409.
///
/// ⚠️⚠️ <b><c>WorkShopId</c> and <c>ElamSenderId</c> have NO FK at all in Legacy</b> — an
/// invalid value is written <em>silently</em>; the central ORA-02291 → 400 mapping does not
/// help here because there is no constraint to violate. No pre-check is added (out of scope,
/// and this project's established pattern leaves race-condition-prone existence checks to real
/// DB constraints wherever one exists — here none does).
///
/// ⚠️⚠️ <b>Two confirmed instances of the phase-12 <c>bool?</c>-should-be-enum bug pattern —
/// flagged, NOT fixed (re-typing would be a breaking API-contract change, out of scope):</b>
/// <list type="bullet">
/// <item><description><c>Case</c> (<c>ELAMH_CASE</c>) — the Oracle column comment literally
/// reads «نوع اعلاميه 1بد 2بس», i.e. the real values are <b>1 and 2</b>, not 0 and 1. A
/// <see cref="bool"/>? round-trip through this column is very likely already wrong.</description></item>
/// <item><description><c>DramadType</c> (<c>ELAMHDRAMAD_TYPE</c>) — the Oracle column comment
/// literally reads « 3حق بيمه نوع اعلاميه 1ذي حسابي 2سايردرآمد», i.e. <b>three</b> values
/// (1, 2, 3). A <see cref="bool"/>? cannot represent this at all — the third value is
/// unreachable through this API.</description></item>
/// </list>
///
/// <c>WebStat</c> (<c>WEB_STAT</c>, comment «ارسال از طريق وب=1-ارسال شده=2») is already
/// correctly modelled as <see cref="byte"/>?; no <c>InclusiveBetween</c> range rule is invented
/// for it — that would be fabricating a business rule. <c>PrintNo</c> (<c>ELAMH_PRINTNO</c>) is
/// <see cref="short"/>? mapped with <c>HasPrecision(5)</c>; no invented range rule beyond what
/// the CLR type already gives.
/// </summary>
/// <param name="VoucherHeadId">Optional link to <c>TB_VOUCHERSHEAD</c> (<c>FK_ELAM_VOUCHER</c>). Maps to <c>VOUCHERSHEAD_ID</c>.</param>
/// <param name="SerialNo">ELAMH_SERIALNO column (optional, max 14 chars; part of <c>AK_AK_ELAMHEAD_ELAMHEAD</c>).</param>
/// <param name="Code">ELAMH_CODE column (optional, max 6 chars; part of <c>AK_AK_ELAMHEAD_ELAMHEAD</c>).</param>
/// <param name="DabirNo">ELAMH_DABIRNO column (optional, max 10 chars — دبيرخانه reference number).</param>
/// <param name="DabirDate">ELAMH_DABIRDATE column (optional, max 8 chars — Legacy string-encoded date, not a real <see cref="DateTime"/>).</param>
/// <param name="PrintNo">ELAMH_PRINTNO column (optional, <see cref="short"/>, <c>HasPrecision(5)</c> — print count).</param>
/// <param name="Case">ELAMH_CASE column (optional <see cref="bool"/>) — see the unverified-enum flag above; real values are 1/2, not 0/1.</param>
/// <param name="SerialNoInput">SERIALNO_INPUT column (optional, max 6 chars — inbound serial number).</param>
/// <param name="WebStat">WEB_STAT column (optional <see cref="byte"/> — already correctly modelled, no invented range rule).</param>
/// <param name="Date">ELAMH_DATE column (optional, max 8 chars — Legacy string-encoded date).</param>
/// <param name="Desc">ELAMH_DESC column (optional, max 300 chars — free-text description).</param>
/// <param name="WorkShopId">WORKSHOP_ID column (optional <see cref="Guid"/>) — ⚠️ NO FK at all; invalid values are written silently.</param>
/// <param name="RcvNo">ELAMH_RCVNO column (optional, max 14 chars).</param>
/// <param name="RcvDt">ELAMH_RCVDT column (optional, max 8 chars — Legacy string-encoded date).</param>
/// <param name="LstMon">ELAMH_LSTMON column (optional, max 2 chars — performance month).</param>
/// <param name="PayNo">PAY_NO column (optional, max 15 chars).</param>
/// <param name="DramadType">ELAMHDRAMAD_TYPE column (optional <see cref="bool"/>) — see the unverified-enum flag above; real values are 1/2/3, a third value unreachable via <see cref="bool"/>?.</param>
/// <param name="PeimanNo">PEIMAN_NO column (optional, max 12 chars — contract number).</param>
/// <param name="WorkShopCode">ELAMH_WORKSHOPCODE column (optional, max 10 chars).</param>
/// <param name="WorkShopName">ELAMH_WORKSHOPNAME column (optional, max 100 chars).</param>
/// <param name="SendRcvVahed">ELAMH_SENDRCVVAHED column (optional, max 4 chars — sending/receiving unit code).</param>
/// <param name="ElamYear">ELAMH_YEAR column (optional, max 2 chars) — distinct from <c>Year</c> below (<c>YEAR</c> column); do not confuse the two.</param>
/// <param name="VahedCode">VAHEDCODE column (optional, max 4 chars; part of <c>AK_AK_ELAMHEAD_ELAMHEAD</c>).</param>
/// <param name="Year">YEAR column (optional, max 4 chars) — distinct from <c>ElamYear</c> above (<c>ELAMH_YEAR</c> column).</param>
/// <param name="ElamSenderId">ELAMSENDERID column (optional <see cref="Guid"/>) — ⚠️ NO FK at all; invalid values are written silently.</param>
public sealed record CreateElamHeadCommand(
    Guid? VoucherHeadId,
    string? SerialNo,
    string? Code,
    string? DabirNo,
    string? DabirDate,
    short? PrintNo,
    bool? Case,
    string? SerialNoInput,
    byte? WebStat,
    string? Date,
    string? Desc,
    Guid? WorkShopId,
    string? RcvNo,
    string? RcvDt,
    string? LstMon,
    string? PayNo,
    bool? DramadType,
    string? PeimanNo,
    string? WorkShopCode,
    string? WorkShopName,
    string? SendRcvVahed,
    string? ElamYear,
    string? VahedCode,
    string? Year,
    Guid? ElamSenderId) : IRequest<Guid>;
