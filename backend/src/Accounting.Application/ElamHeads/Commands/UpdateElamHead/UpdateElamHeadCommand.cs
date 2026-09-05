using MediatR;

namespace Accounting.Application.ElamHeads.Commands.UpdateElamHead;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_ELAMHEAD</c> row (PUT semantics, not
/// PATCH) — the same replace-vs-patch rationale as <c>UpdateRabetCommand</c> applies here (every
/// column is nullable).
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>:
/// identity and creation audit are immutable after insert, and <c>ISDELETED</c> is owned
/// exclusively by <c>DeleteElamHeadCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> are
/// likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
///
/// ⚠️ HEAD ONLY — see <c>CreateElamHeadCommand</c> XML doc for the full explanation of the two
/// unverified <see cref="bool"/>?-should-be-enum columns (<c>Case</c>, <c>DramadType</c>) and
/// the two FK-less columns (<c>WorkShopId</c>, <c>ElamSenderId</c>), all of which apply
/// identically here.
/// </summary>
/// <param name="Id">The <c>TB_ELAMHEAD.ID</c> to update (bound from the route, never the body).</param>
/// <param name="VoucherHeadId">Optional link to <c>TB_VOUCHERSHEAD</c> (<c>FK_ELAM_VOUCHER</c>). Maps to <c>VOUCHERSHEAD_ID</c>.</param>
/// <param name="SerialNo">ELAMH_SERIALNO column (optional, max 14 chars; part of <c>AK_AK_ELAMHEAD_ELAMHEAD</c>).</param>
/// <param name="Code">ELAMH_CODE column (optional, max 6 chars; part of <c>AK_AK_ELAMHEAD_ELAMHEAD</c>).</param>
/// <param name="DabirNo">ELAMH_DABIRNO column (optional, max 10 chars).</param>
/// <param name="DabirDate">ELAMH_DABIRDATE column (optional, max 8 chars).</param>
/// <param name="PrintNo">ELAMH_PRINTNO column (optional, <see cref="short"/>).</param>
/// <param name="Case">ELAMH_CASE column (optional <see cref="bool"/>) — see <c>CreateElamHeadCommand</c> XML doc for the unverified-enum flag.</param>
/// <param name="SerialNoInput">SERIALNO_INPUT column (optional, max 6 chars).</param>
/// <param name="WebStat">WEB_STAT column (optional <see cref="byte"/>).</param>
/// <param name="Date">ELAMH_DATE column (optional, max 8 chars).</param>
/// <param name="Desc">ELAMH_DESC column (optional, max 300 chars).</param>
/// <param name="WorkShopId">WORKSHOP_ID column (optional <see cref="Guid"/>) — ⚠️ NO FK at all.</param>
/// <param name="RcvNo">ELAMH_RCVNO column (optional, max 14 chars).</param>
/// <param name="RcvDt">ELAMH_RCVDT column (optional, max 8 chars).</param>
/// <param name="LstMon">ELAMH_LSTMON column (optional, max 2 chars).</param>
/// <param name="PayNo">PAY_NO column (optional, max 15 chars).</param>
/// <param name="DramadType">ELAMHDRAMAD_TYPE column (optional <see cref="bool"/>) — see <c>CreateElamHeadCommand</c> XML doc for the unverified-enum flag (three real values, only two reachable).</param>
/// <param name="PeimanNo">PEIMAN_NO column (optional, max 12 chars).</param>
/// <param name="WorkShopCode">ELAMH_WORKSHOPCODE column (optional, max 10 chars).</param>
/// <param name="WorkShopName">ELAMH_WORKSHOPNAME column (optional, max 100 chars).</param>
/// <param name="SendRcvVahed">ELAMH_SENDRCVVAHED column (optional, max 4 chars).</param>
/// <param name="ElamYear">ELAMH_YEAR column (optional, max 2 chars) — distinct from <c>Year</c>.</param>
/// <param name="VahedCode">VAHEDCODE column (optional, max 4 chars; part of <c>AK_AK_ELAMHEAD_ELAMHEAD</c>).</param>
/// <param name="Year">YEAR column (optional, max 4 chars) — distinct from <c>ElamYear</c>.</param>
/// <param name="ElamSenderId">ELAMSENDERID column (optional <see cref="Guid"/>) — ⚠️ NO FK at all.</param>
public sealed record UpdateElamHeadCommand(
    Guid Id,
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
    Guid? ElamSenderId) : IRequest;
