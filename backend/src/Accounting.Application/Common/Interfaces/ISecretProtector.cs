namespace Accounting.Application.Common.Interfaces;

/// <summary>رمزگذاری برگشت‌پذیر رازهایی که باید روی سرور بمانند (مثل رمز اسکیمای سیستم قدیم). پیاده‌سازی در Api با Data Protection.</summary>
public interface ISecretProtector
{
    string Protect(string plain);

    /// <summary>null اگر رمزگشایی ممکن نباشد (مثلاً کلیدها عوض شده‌اند) — کاربر باید رمز را دوباره وارد کند.</summary>
    string? Unprotect(string protectedValue);
}
