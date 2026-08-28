using MediatR;

namespace Accounting.Application.ChequeTypes.Commands.UpdateChequeType;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_CHECK_TYPE</c> row (PUT semantics,
/// not PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>. <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c>
/// are likewise absent because the handler sources them from
/// <see cref="Accounting.Application.Common.Interfaces.ICurrentUser"/> and the server clock.
///
/// Field-by-field documentation is intentionally not repeated here — every field (including
/// <c>Id</c>, bound from the route and never the body) has the exact same column/type/length
/// meaning as the identically-named parameter on
/// <see cref="Accounting.Application.ChequeTypes.Commands.CreateChequeType.CreateChequeTypeCommand"/>;
/// see that command's XML doc for full detail (including the <c>ChequeImage</c> size-limit note).
/// No individual <c>&lt;param&gt;</c> tags are used here on purpose — a partial set (documenting
/// only some of the 42 parameters) would itself trigger CS1573 for every undocumented one.
/// </summary>
public sealed record UpdateChequeTypeCommand(
    Guid Id,
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
    string Year,
    string VahedCode) : IRequest;
