using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Elams;

/// <summary>ردیف ورودی اعلامیه: یک معین، مبلغ (بی‌علامت)، تفصیلی‌ها و شناسه.</summary>
public sealed record ElamDetailInput(
    Guid AccountId,
    decimal Amount,
    string? Description,
    string? AttribNo,
    IReadOnlyList<VoucherDetailTafsiliLinkInput>? TafsiliLinks);

/// <summary>یک سطر کارتابل اعلامیه.</summary>
public sealed record ElamCartableItemDto(
    Guid Id,
    ElamKind Kind,
    string? SerialNo,
    string? Date,
    string? Description,
    ElamCase? Case,
    byte? WebStat,
    string? CounterVahedCode,
    string? CounterVahedName,
    string? DabirNo,
    string? DabirDate,
    decimal Amount,
    Guid? VoucherHeadId,
    string? VoucherNumber,
    string? VoucherDate,
    string? VoucherDescription,
    Guid? SenderElamId,
    DaramElamhType? RevenueType);

public sealed record ElamTafsiliViewDto(Guid TafsiliId, Guid LevelId, string? LevelCode, string? Code, string? Name);

public sealed record ElamDetailViewDto(
    Guid Id,
    Guid? AccountId,
    string? AccCode,
    string? AccName,
    decimal Debtor,
    decimal Creditor,
    string? Description,
    string? AttribNo,
    IReadOnlyList<ElamTafsiliViewDto> Tafsilis);

/// <summary>اعلامیه با ردیف‌ها — برای فرم ویرایش و نمایش.</summary>
public sealed record ElamViewDto(
    ElamCartableItemDto Head,
    string? RabetCode,
    string? WorkshopCode,
    string? WorkshopName,
    Guid? WorkshopId,
    string? RcvNo,
    string? RcvDate,
    string? LastMonth,
    string? ElamYear,
    string? PeimanNo,
    string? PayNo,
    bool CanEdit,
    IReadOnlyList<ElamDetailViewDto> Details);

/// <summary>واحد طرف اعلامیه (فهرست انتخاب).</summary>
public sealed record ElamUnitDto(string VahedCode, string VahedName);

/// <summary>حساب رابط اعلامیه از <c>TB_RABET</c>.</summary>
public sealed record ElamRabetAccount(Guid AccountId, string AccCode);

public sealed record ElamCartableFilter(
    string VahedCode,
    string Year,
    ElamKind Kind,
    int PageNumber,
    int PageSize,
    string? SerialFrom,
    string? SerialTo,
    string? DateFrom,
    string? DateTo,
    string? DabirNo,
    string? CounterVahedCode);
