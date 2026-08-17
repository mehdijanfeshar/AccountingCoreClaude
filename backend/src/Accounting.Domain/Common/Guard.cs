using Accounting.Domain.Exceptions;

namespace Accounting.Domain.Common;

/// <summary>
/// اعتبارسنجی‌های مشترک و کوچکی که در چند موجودیت/Value Object تکرار می‌شوند
/// (مثلاً غیرخالی بودن عنوان). این کلاس صرفاً کمکی داخلی برای Domain است و
/// بخشی از Public API این اسمبلی محسوب نمی‌شود.
/// </summary>
internal static class Guard
{
    private const int MaxTitleLength = 200;

    public static string Title(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidTitleException("عنوان نمی‌تواند خالی باشد.");

        var trimmed = value.Trim();

        if (trimmed.Length > MaxTitleLength)
            throw new InvalidTitleException($"عنوان نمی‌تواند بیش از {MaxTitleLength} کاراکتر باشد.");

        return trimmed;
    }
}
