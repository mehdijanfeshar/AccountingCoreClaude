namespace Accounting.Application.ElamHeads.Queries;

/// <summary>
/// Read-side projection of <c>TB_ELAMHEAD</c>. Used by both <c>GetElamHeads</c> (list) and
/// <c>GetElamHeadById</c> — the Domain entity never crosses the Application boundary.
///
/// ⚠️ HEAD ONLY — carries no <c>TB_ELAMDETAIL</c> data (out of scope; see
/// <c>Accounting.Api.Controllers.ElamHeadsController</c> XML doc).
///
/// ⚠️ <c>Case</c>/<c>DramadType</c> are exposed as-is (<see cref="bool"/>?) — see
/// <c>CreateElamHeadCommand</c> XML doc for the confirmed <c>bool?</c>-should-be-enum bug on
/// both columns; re-typing here would be a breaking contract change, deliberately not done.
/// </summary>
/// <param name="Id">ID column.</param>
/// <param name="VoucherHeadId">VOUCHERSHEAD_ID column — optional link to <c>TB_VOUCHERSHEAD</c>.</param>
/// <param name="SerialNo">ELAMH_SERIALNO column — part of <c>AK_AK_ELAMHEAD_ELAMHEAD</c>.</param>
/// <param name="Code">ELAMH_CODE column — part of <c>AK_AK_ELAMHEAD_ELAMHEAD</c>.</param>
/// <param name="DabirNo">ELAMH_DABIRNO column.</param>
/// <param name="DabirDate">ELAMH_DABIRDATE column.</param>
/// <param name="PrintNo">ELAMH_PRINTNO column.</param>
/// <param name="Case">ELAMH_CASE column — ⚠️ unverified <see cref="bool"/>?-should-be-enum, see <c>CreateElamHeadCommand</c> XML doc.</param>
/// <param name="SerialNoInput">SERIALNO_INPUT column.</param>
/// <param name="WebStat">WEB_STAT column.</param>
/// <param name="Date">ELAMH_DATE column.</param>
/// <param name="Desc">ELAMH_DESC column.</param>
/// <param name="WorkShopId">WORKSHOP_ID column — ⚠️ NO FK at all.</param>
/// <param name="RcvNo">ELAMH_RCVNO column.</param>
/// <param name="RcvDt">ELAMH_RCVDT column.</param>
/// <param name="LstMon">ELAMH_LSTMON column.</param>
/// <param name="PayNo">PAY_NO column.</param>
/// <param name="DramadType">ELAMHDRAMAD_TYPE column — ⚠️ unverified <see cref="bool"/>?-should-be-enum (3 real values), see <c>CreateElamHeadCommand</c> XML doc.</param>
/// <param name="PeimanNo">PEIMAN_NO column.</param>
/// <param name="WorkShopCode">ELAMH_WORKSHOPCODE column.</param>
/// <param name="WorkShopName">ELAMH_WORKSHOPNAME column.</param>
/// <param name="SendRcvVahed">ELAMH_SENDRCVVAHED column.</param>
/// <param name="ElamYear">ELAMH_YEAR column — distinct from <c>Year</c>.</param>
/// <param name="VahedCode">VAHEDCODE column — part of <c>AK_AK_ELAMHEAD_ELAMHEAD</c>.</param>
/// <param name="Year">YEAR column — distinct from <c>ElamYear</c>.</param>
/// <param name="ElamSenderId">ELAMSENDERID column — ⚠️ NO FK at all.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp.</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier.</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag. Exposed as-is (including on list results, where rows with
/// <c>IsDeleted == true</c> have already been filtered out) so callers can distinguish a
/// not-deleted row from one that slipped through.
/// </param>
public sealed record ElamHeadDto(
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
    Guid? ElamSenderId,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool? IsDeleted);
