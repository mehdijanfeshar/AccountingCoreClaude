using System.Globalization;
using Accounting.Domain.Accrual;

namespace Accounting.Application.Accrual;

// حسابداری تعهدی، فاز ۶۶ (DDL 090). ثبت سند تعهدی در حسابیار است؛ این ماژول فقط زمان‌بندی، اجرا و لغو را دارد.

public interface IAccrualScheduleRepository
{
    Task AddRangeAsync(IEnumerable<AccrualSchedule> rows, CancellationToken ct);

    /// <summary>
    /// سطر را داخل تراکنش جاری قفل می‌کند (اوراکل <c>FOR UPDATE</c>، با <paramref name="skipLocked"/> <c>SKIP LOCKED</c>) و
    /// tracked برمی‌گرداند. null = نیست، یا (فقط skipLocked) نمونهٔ دیگری قفلش کرده است.
    /// </summary>
    Task<AccrualSchedule?> LockAsync(Guid id, bool skipLocked, CancellationToken ct);

    Task<IReadOnlyList<AccrualSchedule>> ListAsync(string vahedCode, AccrualScheduleFilter filter, CancellationToken ct);

    Task<IReadOnlyList<AccrualSchedule>> ListBySourceAsync(string vahedCode, Guid sourceVoucherId, bool tracking, CancellationToken ct);

    /// <summary>سطرهای باز سررسیدشده (همهٔ واحدها)، قدیمی‌ترین اول — برای کار روزانه.</summary>
    Task<IReadOnlyList<Guid>> ListDueIdsAsync(string todayJalali, int max, CancellationToken ct);

    /// <summary>تعداد سطرهای باز با سررسید تا <paramref name="lastDayJalali"/>، به تفکیک واحد — پیش‌بررسی صورتحساب ماه.</summary>
    Task<IReadOnlyDictionary<string, AccrualOpenCount>> CountOpenDueAsync(string lastDayJalali, CancellationToken ct);

    /// <summary>شمارهٔ سند (<c>DOC_NUM</c>) برای نمایش.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetVoucherNumbersAsync(IReadOnlyCollection<Guid> voucherIds, CancellationToken ct);
}

public sealed record AccrualScheduleFilter(AccrualScheduleState? State, AccrualScheduleKind? Kind, string? FromDue, string? ToDue, int Max = 500);

public sealed record AccrualOpenCount(int Reversals, int Shares)
{
    public int Total => Reversals + Shares;
}

/// <summary>تفصیلی دو طرف سهم ماهانه، در <c>TAFSILI_JSON</c>.</summary>
public sealed record AccrualTafsiliSides(IReadOnlyList<AccrualTafsili> Debit, IReadOnlyList<AccrualTafsili> Credit);

public sealed record AccrualTafsili(int Level, Guid LevelId, Guid DetailId, string? Title);

// ─────────── DTO ───────────

public sealed record AccrualScheduleDto(
    Guid Id, Guid SourceVoucherId, string? SourceVoucherNo, Guid? ExecutionId,
    AccrualScheduleKind Kind, int LineNo, string DueDate, long Amount, string? Description,
    AccrualScheduleState State, Guid? ResultVoucherId, string? ResultVoucherNo,
    string CreatedBy, DateTime CreatedAtUtc);

public sealed record AccrualVoucherScheduleDto(Guid SourceVoucherId, string? SourceVoucherNo, long TotalAmount, IReadOnlyList<AccrualScheduleDto> Lines);

public sealed record AccrualRunResultDto(Guid VoucherId, string? VoucherNo, bool AlreadyDone);

public sealed record AccrualCancelResultDto(int CancelledCount, long CancelledAmount);

/// <summary>تاریخ شمسی <c>yyyyMMdd</c> (قالب DATE_DOC).</summary>
public static class AccrualDates
{
    private static readonly PersianCalendar Calendar = new();

    public static string ToJalali(DateOnly date)
    {
        var d = date.ToDateTime(TimeOnly.MinValue);
        return $"{Calendar.GetYear(d):0000}{Calendar.GetMonth(d):00}{Calendar.GetDayOfMonth(d):00}";
    }

    public static string Today() => ToJalali(DateOnly.FromDateTime(DateTime.Now));

    public static DateOnly ToDate(string jalali) =>
        DateOnly.FromDateTime(Calendar.ToDateTime(int.Parse(jalali[..4]), int.Parse(jalali[4..6]), int.Parse(jalali[6..8]), 0, 0, 0, 0));

    /// <summary>روز اول ماه بعد.</summary>
    public static string FirstDayOfNextMonth(int year, int month) =>
        month == 12 ? $"{year + 1:0000}0101" : $"{year:0000}{month + 1:00}01";

    /// <summary>روز آخر ماه (<paramref name="offset"/> ماه بعد از ماه داده‌شده).</summary>
    public static string LastDayOfMonth(int year, int month, int offset = 0)
    {
        var index = year * 12 + (month - 1) + offset;
        var y = index / 12;
        var m = index % 12 + 1;
        return $"{y:0000}{m:00}{Calendar.GetDaysInMonth(y, m):00}";
    }

    /// <summary>ورودی کاربر: <c>1405/08/01</c> یا <c>14050801</c>؛ نامعتبر ⇒ null.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
        return digits.Length == 8 ? digits : null;
    }
}
