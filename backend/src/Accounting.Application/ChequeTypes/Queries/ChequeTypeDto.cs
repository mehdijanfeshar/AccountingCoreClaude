namespace Accounting.Application.ChequeTypes.Queries;

/// <summary>
/// Read-side projection of <c>TB_CHECK_TYPE</c>. Used by both <c>GetChequeTypes</c> (list) and
/// <c>GetChequeTypeById</c> — the Domain entity never crosses the Application boundary. Every
/// layout/printer field mirrors the identically-named column documented on
/// <see cref="Accounting.Application.ChequeTypes.Commands.CreateChequeType.CreateChequeTypeCommand"/>;
/// full column documentation is not repeated here. <c>Id</c> is the ID column; <c>CreatedDate</c>/
/// <c>UpdatedDate</c>/<c>AddUserId</c>/<c>ChangeUserId</c> are the audit trail; <c>IsDeleted</c> is
/// the logical delete flag, exposed as-is. No individual <c>&lt;param&gt;</c> tags are used here on
/// purpose — a partial set (documenting only some of the 46 parameters) would itself trigger
/// CS1573 for every undocumented one.
/// </summary>
public sealed record ChequeTypeDto(
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
    string VahedCode,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
