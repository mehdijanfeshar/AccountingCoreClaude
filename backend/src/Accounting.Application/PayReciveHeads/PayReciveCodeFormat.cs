namespace Accounting.Application.PayReciveHeads;

/// <summary>
/// ریسک ۲-ب: کد دریافت/پرداخت در مرجع سمت سرور به ۵ رقم با صفر چپ (<c>PadLeft(5,'0')</c>) یکسان می‌شود،
/// تا «12» و «00012» دو کد جدا نشوند.
/// </summary>
public static class PayReciveCodeFormat
{
    public static string? Normalize(string? code)
    {
        var trimmed = code?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.All(char.IsDigit) ? trimmed.PadLeft(5, '0') : trimmed;
    }
}
