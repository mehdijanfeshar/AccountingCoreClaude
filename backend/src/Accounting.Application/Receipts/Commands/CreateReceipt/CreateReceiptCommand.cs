using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Receipts.Commands.CreateReceipt;

/// <summary>
/// Creates a new <c>TB_RECEIP</c> row (Legacy bank receipt/transfer document — a فيش or حواله
/// that <c>TB_BANKCARTDETAIL</c>, <c>TB_PAYRECIVDETAIL</c> and <c>TB_VOUCHERSDETAIL</c> rows can
/// link back to). Carries primitive fields only — the handler is responsible for constructing
/// the Domain entity. Returns the newly generated <see cref="Guid"/> ID.
///
/// <c>TB_RECEIP</c> has NO UNIQUE constraint and NO foreign keys at all in
/// <c>LegacyDbContext</c> — see <c>Accounting.Api.Controllers.ReceiptsController</c> XML doc for
/// why 409 is never declared and why 400-from-FK cannot arise here.
///
/// ⚠️ <c>ReceiptKind</c> is <c>NUMBER(1)</c> mapped as a non-nullable <see cref="bool"/>, and its
/// name says <em>kind</em> (فيش‌ vs حواله, mirroring <c>TB_BANKCARTDETAIL.CHECKRECEIPTTYPE</c>),
/// not a true/false flag — a strong candidate for the same multi-valued-enum bug pattern
/// confirmed elsewhere in this schema (CLAUDE.md phase 12). Flagged here, not fixed; the CLR type
/// is deliberately left as <see cref="bool"/>.
///
/// ⚠️ Three other tables point AT this one (<c>TB_BANKCARTDETAIL</c>, <c>TB_PAYRECIVDETAIL</c>,
/// <c>TB_VOUCHERSDETAIL</c>). Soft-deleting a receipt leaves any referencing rows active and
/// silent — no cascade is implemented here, and none was asked for.
/// </summary>
/// <param name="ReceiptKind">RECEIPT_KIND column — see the unverified-enum note above.</param>
/// <param name="ReceiptDate">RECEIPT_DATE column (required, max 8 chars).</param>
/// <param name="ReceiptNo">RECEIPT_NO column (required, max 8 chars).</param>
/// <param name="DateRsid">DATE_RSID column (optional, max 8 chars).</param>
/// <param name="Year">Fiscal year (required, max 4 chars).</param>
public sealed record CreateReceiptCommand(
    bool ReceiptKind,
    string ReceiptDate,
    string ReceiptNo,
    string? DateRsid,
    string Year) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (required, max 4 chars). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>CreateReceiptCommandHandler</c>. See <see cref="IVahedScopedCommand"/>
    /// for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
