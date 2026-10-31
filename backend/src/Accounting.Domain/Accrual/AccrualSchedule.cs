namespace Accounting.Domain.Accrual;

// حسابداری تعهدی، فاز ۶۶ (DDL 090) — طرح: docs/accrual-phase66-task.md. POCO خالص؛ قواعد در Application.

/// <summary>نوع سطر زمان‌بندی: ۱ برگشت برآورد، ۲ سهم ماهانهٔ پیش‌پرداخت، ۳ سهم ماهانهٔ درآمد انتقالی.</summary>
public enum AccrualScheduleKind { Reversal = 1, Prepaid = 2, Deferred = 3 }

/// <summary>وضعیت سطر: ۱ باز، ۲ اجراشده (سند ساخته شد)، ۳ لغو.</summary>
public enum AccrualScheduleState { Open = 1, Done = 2, Cancelled = 3 }

/// <summary>
/// یک سطر <c>TB_AC_SCHEDULE</c>: کاری که در <see cref="DueDate"/> باید یک سند بسازد — برگشت سند اصلی
/// (<see cref="AccrualScheduleKind.Reversal"/>) یا سهم یک ماه (<see cref="DebitAccountId"/>/<see cref="CreditAccountId"/>).
/// یکتایی (SOURCE_VOUCHER_ID, KIND, LINE_NO) آخرین سد اجرای دوباره است.
/// </summary>
public class AccrualSchedule
{
    public Guid Id { get; set; }
    public string VahedCode { get; set; } = "";
    /// <summary>سال سررسید (چهار رقم اول <see cref="DueDate"/>).</summary>
    public string Year { get; set; } = "";
    /// <summary>سند اصلی (<c>TB_VOUCHERSHEAD.ID</c>).</summary>
    public Guid SourceVoucherId { get; set; }
    /// <summary>اجرای حسابیار (<c>TB_OP_EXECUTION.ID</c>) اگر از الگو آمده.</summary>
    public Guid? ExecutionId { get; set; }
    public AccrualScheduleKind Kind { get; set; }
    public int LineNo { get; set; }
    /// <summary>سررسید شمسی <c>yyyyMMdd</c> = تاریخ سند ساخته‌شده.</summary>
    public string DueDate { get; set; } = "";
    /// <summary>ریال صحیح، بزرگ‌تر از صفر.</summary>
    public long Amount { get; set; }
    public Guid? DebitAccountId { get; set; }
    public Guid? CreditAccountId { get; set; }
    /// <summary>تفصیلی دو طرف سهم ماهانه (JSON)؛ شکلش در Application.</summary>
    public string? TafsiliJson { get; set; }
    public string? Description { get; set; }
    public AccrualScheduleState State { get; set; } = AccrualScheduleState.Open;
    public Guid? ResultVoucherId { get; set; }
    public bool IsDeleted { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>
/// تقسیم مبلغ روی چند ماه به ریال صحیح: همهٔ سطرها سهم برابر (کف تقسیم)، باقیماندهٔ گرد روی <b>آخرین</b> سطر؛
/// جمع سطرها همیشه برابر مبلغ اصلی.
/// </summary>
public static class AccrualScheduleSplitter
{
    public static IReadOnlyList<long> Split(long total, int parts)
    {
        if (parts < 1) throw new ArgumentOutOfRangeException(nameof(parts), "تعداد ماه باید حداقل ۱ باشد.");
        if (total < parts) throw new ArgumentOutOfRangeException(nameof(total), "مبلغ باید حداقل یک ریال برای هر ماه باشد.");

        var share = total / parts;
        var result = new long[parts];
        for (var i = 0; i < parts - 1; i++) result[i] = share;
        result[^1] = total - share * (parts - 1);
        return result;
    }
}

/// <summary>قراردادهای الگوی تعهدی در حسابیار (بدون ستون جدید).</summary>
public static class AccrualConventions
{
    /// <summary>
    /// ردیف «سهم ماهانه» در الگوی تقسیم چندماهه با این کلید شرط علامت می‌خورد: در سند اصلی ساخته نمی‌شود
    /// (موتور ردیف شرطی بی‌جواب را رد می‌کند) و فقط حساب/تفصیلی سهم هر ماه را تعیین می‌کند.
    /// </summary>
    public const string ScheduleLineKey = "@accrual";
    public const string ScheduleLineValue = "monthly";

    /// <summary>پارامتر الزامی الگوی تقسیم چندماهه: تعداد ماه (۱ تا <see cref="MaxMonths"/>).</summary>
    public const string MonthsParameterKey = "months";

    public const int MaxMonths = 120;
}
