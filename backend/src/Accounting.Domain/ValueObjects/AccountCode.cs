using Accounting.Domain.Exceptions;

namespace Accounting.Domain.ValueObjects;

/// <summary>
/// کد یک حساب با طول ثابت و فقط شامل ارقام. طول موردانتظار هنگام فراخوانی
/// <see cref="Create(string?, int)"/> به‌صورت پارامتر داده می‌شود.
/// این Value Object تضمین می‌کند که هرگز یک کد خالی، با طول نادرست یا شامل کاراکتر
/// غیرمجاز در سیستم وجود نداشته باشد.
/// <para>
/// یادداشت (۲۰۲۶-۰۸-۱۷): سلسله‌مراتب ثابت گروه/کل/معین که این تایپ در اصل برای آن
/// ساخته شده بود حذف شد (تصمیم «Legacy جایگزین کامل»). خودِ این Value Object منسوخ
/// نیست و برای اعتبارسنجی کد حساب در مدل مبتنی بر Legacy قابل استفاده است.
/// </para>
/// </summary>
public sealed class AccountCode : IEquatable<AccountCode>
{
    public string Value { get; }

    private AccountCode(string value)
    {
        Value = value;
    }

    public static AccountCode Create(string? value, int expectedLength)
    {
        if (expectedLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedLength), "طول مجاز کد باید مثبت باشد.");

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidAccountCodeException("کد حساب نمی‌تواند خالی باشد.");

        var trimmed = value.Trim();

        if (trimmed.Length != expectedLength)
        {
            throw new InvalidAccountCodeException(
                $"طول کد «{trimmed}» باید دقیقاً {expectedLength} کاراکتر باشد (طول فعلی: {trimmed.Length}).");
        }

        if (!trimmed.All(char.IsDigit))
            throw new InvalidAccountCodeException($"کد حساب «{trimmed}» فقط می‌تواند شامل ارقام ۰ تا ۹ باشد.");

        return new AccountCode(trimmed);
    }

    public override string ToString() => Value;

    public bool Equals(AccountCode? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as AccountCode);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(AccountCode? left, AccountCode? right) => Equals(left, right);

    public static bool operator !=(AccountCode? left, AccountCode? right) => !Equals(left, right);
}
