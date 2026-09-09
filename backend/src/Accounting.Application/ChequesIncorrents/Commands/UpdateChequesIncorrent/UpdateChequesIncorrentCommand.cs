using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
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
///
/// ⚠️ <b>Scope note:</b> <see cref="IVahedScopedCommand"/> here only guarantees that
/// <c>VAHEDCODE</c> cannot be *changed* to an arbitrary unit by the caller. It does
/// <b>not</b> check whether the caller is allowed to touch this particular row in the first
/// place — record-ownership verification on Update is explicitly out of scope for this pass, by
/// project-owner decision.
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
    string Year) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (required, max 4 chars). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>UpdateChequesIncorrentCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism, and the scope note above for
    /// what this does <b>not</b> cover.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
