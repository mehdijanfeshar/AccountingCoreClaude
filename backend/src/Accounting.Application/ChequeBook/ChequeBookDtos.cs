using Accounting.Domain.ValueObjects;

namespace Accounting.Application.ChequeBook;

/// <summary>یک چک استفاده‌شده در سند — سطر دفتر چک.</summary>
public sealed record ChequeBookItemDto(
    Guid CheckId,
    Guid VoucherDetailId,
    Guid VoucherHeadId,
    string ChequeNo,
    string? ChequeDate,
    string? PayTo,
    string? PaperDescription,
    string? LineDescription,
    decimal Amount,
    string? AccountNumber,
    string? BankName,
    string? CheckBookTitle,
    string? VoucherNumber,
    string? VoucherDate,
    int? DocLife,
    bool IsCanceled,
    bool IsPrinted,
    ChequeApprovalState? ApprovalState,
    string? PreparedBy,
    string? AccountingBy,
    string? ManagerBy,
    string? ApprovalNote);

public sealed record ChequeBookFilter(
    string VahedCode,
    string Year,
    int PageNumber,
    int PageSize,
    Guid? BankAccountId,
    string? FromDate,
    string? ToDate,
    bool? Canceled,
    bool? Printed,
    string? ChequeNo,
    decimal? Amount,
    string? Description,
    ChequeApprovalState? ApprovalState,
    bool OnlyUnissued);

/// <summary>چک قابل انتخاب در فرم سند (ابطال‌نشده و بدون ردیف سند فعال).</summary>
public sealed record AvailableChequeDto(
    Guid CheckId,
    string ChequeNo,
    string? CheckBookTitle,
    Guid BankAccountId,
    string? AccountNumber,
    string? BankName);

public sealed record ChequeApprovalEventDto(
    ChequeApprovalAction Action,
    ChequeApprovalState? FromState,
    ChequeApprovalState ToState,
    string UserId,
    string? Note,
    DateTime CreatedDate);

/// <summary>داده‌های چاپ یک چک روی برگ چک (با تنظیمات محیطی نوع دسته‌چک).</summary>
public sealed record ChequePrintDto(
    Guid CheckId,
    string ChequeNo,
    string? ChequeDate,
    string? PayTo,
    string? PaperDescription,
    decimal Amount,
    string? AccountNumber,
    string? BankName,
    Guid? ChequeTypeId,
    string? ChequeTypeTitle,
    int? Width,
    int? Height,
    int? MarginTop,
    int? MarginLeft,
    byte[]? Image,
    IReadOnlyList<ChequeFieldLayoutDto>? Fields = null);

/// <summary>
/// جای یک فیلد روی برگ چک از ستون‌های <c>TB_CHECK_TYPE.CHEQUE_*</c> (میلی‌متر؛ قلم = اندازه به pt). فقط فیلدهایی که
/// دست‌کم یک مختصات دارند؛ بقیه جای پیش‌فرض قالب مرجع را می‌گیرند. کلیدها: NDATE تاریخ عددی، ADATE تاریخ حروفی،
/// AAMOUNT مبلغ حروفی، NAMOUNT مبلغ عددی، DESCRIBE1 در وجه، DESCRIBE2 بابت.
/// </summary>
public sealed record ChequeFieldLayoutDto(string Key, int? Left, int? Top, int? Width, string? Font);

/// <summary>دسته‌چک صوری قابل انتخاب در فرم سند؛ شمارهٔ بعدی هنگام ثبت سند صادر می‌شود. NextNumber null = پر شده.</summary>
public sealed record SoriChequeBookDto(
    Guid CheckBookId,
    string? CheckBookTitle,
    Guid BankAccountId,
    string? AccountNumber,
    string? BankName,
    string FromNumber,
    string ToNumber,
    string? NextNumber);

/// <summary>یک برگ چک در «اوراق چک» دسته‌چک. <see cref="PaperDescription"/> = بابت.</summary>
public sealed record ChequeLeafDto(
    Guid CheckId,
    string ChequeNo,
    string? ChequeDate,
    string? PayTo,
    string? PaperDescription,
    bool IsCanceled,
    bool IsPrinted,
    Guid? VoucherHeadId,
    string? VoucherNumber,
    string? VoucherDate,
    decimal? Amount,
    Accounting.Domain.ValueObjects.ChequeApprovalState? ApprovalState);
