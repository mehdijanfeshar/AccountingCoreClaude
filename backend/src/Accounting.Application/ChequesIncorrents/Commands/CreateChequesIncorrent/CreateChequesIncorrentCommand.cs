using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.ChequesIncorrents.Commands.CreateChequesIncorrent;

/// <summary>
/// Creates a new <c>TB_CHEQUES_INCORRENT</c> row (Legacy "dishonored/incorrect cheques"
/// register). Carries primitive fields only — the handler is responsible for constructing the
/// Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// ⚠️ <c>CheckId</c> has NO FK at all in the Legacy schema (there is no <c>HasOne</c> for it in
/// <c>LegacyDbContext</c>), even though it looks like it should reference <c>TB_CHECK</c> — so a
/// value that references no real row is written silently and the central FK→400 mapping (used
/// elsewhere in this project) does not apply here at all. Flagged (not fixed) here: no
/// pre-check is added, per project convention.
///
/// ⚠️ <c>Creditor</c> is an amount column mapped as non-nullable <see cref="decimal"/>
/// (<c>NUMBER(25)</c>). The open project decision "amount data type: <c>long</c> vs
/// <c>decimal?</c>" (see <c>docs/open-decisions.md</c>/CLAUDE.md open items) applies to this
/// column too — no range/precision rule is invented here.
/// </summary>
/// <param name="CheckId">Optional reference to a cheque — see the no-FK warning above.</param>
/// <param name="DocNum">Real document number (شماره واقعی سند) (required, max 6 chars).</param>
/// <param name="DocDate">Document date (تاریخ سند), Legacy string format (required, max 8 chars).</param>
/// <param name="CheqNo">Cheque/payment number (شماره پرداخت) (required, max 8 chars).</param>
/// <param name="CheqDate">Cheque creation date (تاریخ ایجاد), Legacy string format (required, max 8 chars).</param>
/// <param name="PaperDesc">Optional description (بابت) (max 200 chars).</param>
/// <param name="PayTo">Optional payee (دروجه) (max 200 chars).</param>
/// <param name="RecivDate">Optional execution date (تاریخ اجرا), Legacy string format (max 8 chars).</param>
/// <param name="AccountNumber">Current account number (شماره جاری) (required, max 13 chars).</param>
/// <param name="Creditor">Creditor amount (بستانکار) — required, non-nullable <c>NUMBER(25)</c>; see the amount-type warning above.</param>
/// <param name="Year">Cheque usage year (سال استفاده از چک) (required, max 4 chars).</param>
public sealed record CreateChequesIncorrentCommand(
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
    string Year) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (کد واحد) (required, max 4 chars). Never bound from the request
    /// body — <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the
    /// Swagger schema — and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated caller's
    /// own unit code before the request reaches <c>CreateChequesIncorrentCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
