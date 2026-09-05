using MediatR;

namespace Accounting.Application.ChequesIncorrents.Commands.UpdateChequesIncorrent;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_CHEQUES_INCORRENT</c> row (PUT
/// semantics, not PATCH) — the same replace-vs-patch rationale as <c>UpdateWorkShopCommand</c>
/// applies here.
///
/// Deliberately excludes <c>ID</c>, <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>:
/// identity and creation audit are immutable after insert, and <c>ISDELETED</c> is owned
/// exclusively by <c>DeleteChequesIncorrentCommand</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c>
/// are likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock,
/// never from client input.
/// </summary>
/// <param name="Id">The <c>TB_CHEQUES_INCORRENT.ID</c> to update (bound from the route, never the body).</param>
/// <param name="CheckId">Optional reference to a cheque — see <c>CreateChequesIncorrentCommand</c> XML doc for the no-FK warning.</param>
/// <param name="DocNum">Real document number (required, max 6 chars).</param>
/// <param name="DocDate">Document date, Legacy string format (required, max 8 chars).</param>
/// <param name="CheqNo">Cheque/payment number (required, max 8 chars).</param>
/// <param name="CheqDate">Cheque creation date, Legacy string format (required, max 8 chars).</param>
/// <param name="PaperDesc">Optional description (max 200 chars).</param>
/// <param name="PayTo">Optional payee (max 200 chars).</param>
/// <param name="RecivDate">Optional execution date, Legacy string format (max 8 chars).</param>
/// <param name="AccountNumber">Current account number (required, max 13 chars).</param>
/// <param name="Creditor">Creditor amount — required, non-nullable <c>NUMBER(25)</c>; see <c>CreateChequesIncorrentCommand</c> XML doc for the amount-type open question.</param>
/// <param name="VahedCode">Organizational unit code (required, max 4 chars).</param>
/// <param name="Year">Cheque usage year (required, max 4 chars).</param>
public sealed record UpdateChequesIncorrentCommand(
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
    string Year) : IRequest;
