namespace Accounting.Application.ChequesIncorrents.Queries;

/// <summary>
/// Read-side projection of <c>TB_CHEQUES_INCORRENT</c>. Used by both <c>GetChequesIncorrents</c>
/// (list) and <c>GetChequesIncorrentById</c> — the Domain entity never crosses the Application
/// boundary.
/// </summary>
/// <param name="Id">ID column (PK).</param>
/// <param name="CheckId">CHECK_ID column — optional; see <c>CreateChequesIncorrentCommand</c> XML doc for the no-FK warning.</param>
/// <param name="DocNum">DOC_NUM column — real document number.</param>
/// <param name="DocDate">DOC_DATE column — Legacy string date.</param>
/// <param name="CheqNo">CHEQ_NO column — cheque/payment number.</param>
/// <param name="CheqDate">CHEQ_DATE column — Legacy string date.</param>
/// <param name="PaperDesc">PAPER_DESC column — optional description.</param>
/// <param name="PayTo">PAYTO column — optional payee.</param>
/// <param name="RecivDate">RECIVDATE column — optional execution date, Legacy string format.</param>
/// <param name="AccountNumber">ACCOUNTNUMBER column — current account number.</param>
/// <param name="Creditor">CREDITOR column — non-nullable amount; see <c>CreateChequesIncorrentCommand</c> XML doc for the amount-type open question.</param>
/// <param name="VahedCode">VAHEDCODE column — organizational unit code.</param>
/// <param name="Year">YEAR column — cheque usage year.</param>
/// <param name="CreatedDate">Audit trail: creation timestamp (non-nullable on this table).</param>
/// <param name="UpdatedDate">Audit trail: last update timestamp.</param>
/// <param name="AddUserId">Audit trail: creating user identifier (non-nullable on this table).</param>
/// <param name="ChangeUserId">Audit trail: last modifying user identifier.</param>
/// <param name="IsDeleted">
/// Logical delete flag (non-nullable <see cref="bool"/> on this table). Exposed as-is (including
/// on list results, where deleted rows have already been filtered out) so callers can
/// distinguish a not-deleted row from one that slipped through.
/// </param>
public sealed record ChequesIncorrentDto(
    Guid Id,
    Guid? CheckId,
    string DocNum,
    string DocDate,
    string CheqNo,
    string CheqDate,
    string? PaperDesc,
    string? PayTo,
    string? RecivDate,
    string AccountNumber,
    decimal Creditor,
    string VahedCode,
    string Year,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    string AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
