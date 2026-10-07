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
    short? ChequeWidth,
    short? ChequeHeight,
    byte[]? ChequeImage,
    string? ChequeAdateFont,
    short? ChequeAdateLeft,
    short? ChequeAdateTop,
    short? ChequeAdateWidth,
    string? ChequeNdateFont,
    short? ChequeNdateLeft,
    short? ChequeNdateTop,
    short? ChequeNdateWidth,
    string? ChequeAamountFont,
    short? ChequeAamountLeft,
    short? ChequeAamountTop,
    short? ChequeAamountWidth,
    string? ChequeLamountFont,
    short? ChequeLamountLeft,
    short? ChequeLamountTop,
    short? ChequeLamountWidth,
    string? ChequeNamountFont,
    short? ChequeNamountLeft,
    short? ChequeNamountTop,
    short? ChequeNamountWidth,
    string? ChequeDescribe1Font,
    short? ChequeDescribe1Left,
    short? ChequeDescribe1Top,
    short? ChequeDescribe1Width,
    string? ChequeDescribe2Font,
    short? ChequeDescribe2Left,
    short? ChequeDescribe2Top,
    short? ChequeDescribe2Width,
    string? ChequeBreaklineFont,
    short? ChequeBreaklineLeft,
    short? ChequeBreaklineTop,
    short? ChequeBreaklineWidth,
    short? PrinterMargineTop,
    short? PrinterMargineLeft,
    string? PrinterType,
    string Year,
    string VahedCode,
    DateTime? CreatedDate,
    DateTime? UpdatedDate,
    string? AddUserId,
    string? ChangeUserId,
    bool IsDeleted);
