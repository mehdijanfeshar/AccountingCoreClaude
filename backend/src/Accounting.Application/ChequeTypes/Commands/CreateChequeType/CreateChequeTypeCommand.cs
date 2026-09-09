using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.ChequeTypes.Commands.CreateChequeType;

/// <summary>
/// Creates a new <c>TB_CHECK_TYPE</c> row (a legacy cheque-print layout/configuration template —
/// title, image, and per-field font/left/top/width layout metadata for each printable region of
/// a physical cheque, plus printer margins/type). Carries primitive fields only — the handler is
/// responsible for constructing the Domain entity. Returns the newly generated <see cref="Guid"/>
/// ID.
///
/// No UNIQUE constraint exists on this table, so no 409 is ever produced by
/// <c>UnitOfWork.SaveChangesAsync</c> for this entity — see
/// <c>Accounting.Api.Controllers.ChequeTypesController</c> XML doc. No FK exists on this table's
/// own columns either.
/// </summary>
/// <param name="ChequeTypeTitle">CHEQUE_TYPE_TITLE column (max 25 chars, optional).</param>
/// <param name="ChequeWidth">CHEQUE_WIDTH column (<c>NUMBER(3)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeHeight">CHEQUE_HEIGHT column (<c>NUMBER(3)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeImage">
/// CHEQUE_IMAGE column, mapped to Oracle <c>BLOB</c> (JSON base64 on the wire). ⚠️ No request-size
/// or upload-size-limit policy is enforced at the API layer for this field — that is an unmade
/// decision, deliberately not guessed here (flagging it rather than inventing a limit).
/// </param>
/// <param name="ChequeAdateFont">CHEQUE_ADATE_FONT column (max 200 chars, optional).</param>
/// <param name="ChequeAdateLeft">CHEQUE_ADATE_LEFT column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeAdateTop">CHEQUE_ADATE_TOP column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeAdateWidth">CHEQUE_ADATE_WIDTH column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeNdateFont">CHEQUE_NDATE_FONT column (max 200 chars, optional).</param>
/// <param name="ChequeNdateLeft">CHEQUE_NDATE_LEFT column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeNdateTop">CHEQUE_NDATE_TOP column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeNdateWidth">CHEQUE_NDATE_WIDTH column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeAamountFont">CHEQUE_AAMOUNT_FONT column (max 200 chars, optional).</param>
/// <param name="ChequeAamountLeft">CHEQUE_AAMOUNT_LEFT column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeAamountTop">CHEQUE_AAMOUNT_TOP column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeAamountWidth">CHEQUE_AAMOUNT_WIDTH column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeLamountFont">CHEQUE_LAMOUNT_FONT column (max 200 chars, optional).</param>
/// <param name="ChequeLamountLeft">CHEQUE_LAMOUNT_LEFT column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeLamountTop">CHEQUE_LAMOUNT_TOP column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeLamountWidth">CHEQUE_LAMOUNT_WIDTH column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeNamountFont">CHEQUE_NAMOUNT_FONT column (max 200 chars, optional).</param>
/// <param name="ChequeNamountLeft">CHEQUE_NAMOUNT_LEFT column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeNamountTop">CHEQUE_NAMOUNT_TOP column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeNamountWidth">CHEQUE_NAMOUNT_WIDTH column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeDescribe1Font">CHEQUE_DESCRIBE1_FONT column (max 200 chars, optional).</param>
/// <param name="ChequeDescribe1Left">CHEQUE_DESCRIBE1_LEFT column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeDescribe1Top">CHEQUE_DESCRIBE1_TOP column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeDescribe1Width">CHEQUE_DESCRIBE1_WIDTH column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeDescribe2Font">CHEQUE_DESCRIBE2_FONT column (max 200 chars, optional).</param>
/// <param name="ChequeDescribe2Left">CHEQUE_DESCRIBE2_LEFT column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeDescribe2Top">CHEQUE_DESCRIBE2_TOP column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeDescribe2Width">CHEQUE_DESCRIBE2_WIDTH column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeBreaklineFont">CHEQUE_BREAKLINE_FONT column (max 200 chars, optional).</param>
/// <param name="ChequeBreaklineLeft">CHEQUE_BREAKLINE_LEFT column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeBreaklineTop">CHEQUE_BREAKLINE_TOP column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="ChequeBreaklineWidth">CHEQUE_BREAKLINE_WIDTH column (<c>NUMBER(4)</c>, mapped as <c>byte?</c>).</param>
/// <param name="PrinterMargineTop">PRINTER_MARGINE_TOP column (<c>NUMBER(3)</c>, mapped as <c>byte?</c>).</param>
/// <param name="PrinterMargineLeft">PRINTER_MARGINE_LEFT column (<c>NUMBER(3)</c>, mapped as <c>byte?</c>).</param>
/// <param name="PrinterType">PRINTER_TYPE column (max 100 chars, optional).</param>
/// <param name="Year">YEAR column (max 4 chars, fixed-length, required fiscal year).</param>
public sealed record CreateChequeTypeCommand(
    string? ChequeTypeTitle,
    byte? ChequeWidth,
    byte? ChequeHeight,
    byte[]? ChequeImage,
    string? ChequeAdateFont,
    byte? ChequeAdateLeft,
    byte? ChequeAdateTop,
    byte? ChequeAdateWidth,
    string? ChequeNdateFont,
    byte? ChequeNdateLeft,
    byte? ChequeNdateTop,
    byte? ChequeNdateWidth,
    string? ChequeAamountFont,
    byte? ChequeAamountLeft,
    byte? ChequeAamountTop,
    byte? ChequeAamountWidth,
    string? ChequeLamountFont,
    byte? ChequeLamountLeft,
    byte? ChequeLamountTop,
    byte? ChequeLamountWidth,
    string? ChequeNamountFont,
    byte? ChequeNamountLeft,
    byte? ChequeNamountTop,
    byte? ChequeNamountWidth,
    string? ChequeDescribe1Font,
    byte? ChequeDescribe1Left,
    byte? ChequeDescribe1Top,
    byte? ChequeDescribe1Width,
    string? ChequeDescribe2Font,
    byte? ChequeDescribe2Left,
    byte? ChequeDescribe2Top,
    byte? ChequeDescribe2Width,
    string? ChequeBreaklineFont,
    byte? ChequeBreaklineLeft,
    byte? ChequeBreaklineTop,
    byte? ChequeBreaklineWidth,
    byte? PrinterMargineTop,
    byte? PrinterMargineLeft,
    string? PrinterType,
    string Year) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// Organizational unit code (required, max 4 chars). Never bound from the request body —
    /// <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and the Swagger
    /// schema — and never trusted even if a caller manages to set it: <c>VahedScopeBehavior</c>
    /// unconditionally overwrites this with the authenticated caller's own unit code before the
    /// request reaches <c>CreateChequeTypeCommandHandler</c>. See <see cref="IVahedScopedCommand"/>
    /// for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
