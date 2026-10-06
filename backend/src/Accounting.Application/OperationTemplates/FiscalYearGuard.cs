using System.Globalization;

namespace Accounting.Application.OperationTemplates;

/// <summary>
/// سال مالی سند = سال شمسی تاریخ سند (همان قرارداد <c>YEAR = DATE_DOC[..4]</c> سندهای خودکار پروژه).
/// سال مالی جاری کاربر مثل بقیهٔ endpointها از query (<c>?year=</c>) می‌آید؛ تاریخی بیرون از آن رد می‌شود
/// تا سند «امروز» در سالی که کاربر در آن کار نمی‌کند ثبت نشود.
/// </summary>
public static class FiscalYearGuard
{
    public static string JalaliYear(DateOnly date) =>
        new PersianCalendar().GetYear(date.ToDateTime(TimeOnly.MinValue)).ToString("0000", CultureInfo.InvariantCulture);

    /// <summary>نمایش شمسی <c>yyyy/MM/dd</c> برای شرح سند.</summary>
    public static string Jalali(DateOnly date)
    {
        var pc = new PersianCalendar();
        var dt = date.ToDateTime(TimeOnly.MinValue);
        return string.Create(CultureInfo.InvariantCulture, $"{pc.GetYear(dt):0000}/{pc.GetMonth(dt):00}/{pc.GetDayOfMonth(dt):00}");
    }

    /// <returns>خطا (کلید <c>voucherDate</c>) یا null وقتی سال مالی مشخص نیست یا تاریخ در آن است.</returns>
    public static EngineError? Check(DateOnly date, string? fiscalYear)
    {
        if (string.IsNullOrWhiteSpace(fiscalYear) || JalaliYear(date) == fiscalYear.Trim()) return null;
        return new EngineError(EngineErrorCode.InvalidParameterValue,
            $"تاریخ سند در سال مالی {fiscalYear.Trim()} نیست؛ سند فقط در سال مالی جاری شما ثبت می‌شود.",
            "voucherDate", $"سند به چه تاریخی از سال {fiscalYear.Trim()} ثبت شود؟");
    }
}
