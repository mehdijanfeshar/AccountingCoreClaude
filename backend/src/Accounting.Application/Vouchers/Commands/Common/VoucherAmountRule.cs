using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.Common;

/// <summary>
/// مبلغ ردیف سند: ریال صحیح و نامنفی (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۷، ریسک #۴). نوع ستون
/// <c>decimal?</c> می‌ماند؛ فقط اعشار و منفی رد می‌شوند. <c>null</c> مجاز است (= صفر).
/// </summary>
public static class VoucherAmountRule
{
    public static IRuleBuilderOptions<T, decimal?> WholeRialAmount<T>(this IRuleBuilder<T, decimal?> rule, string label)
        => rule
            .Must(v => v is null || (v.Value >= 0 && decimal.Truncate(v.Value) == v.Value))
            .WithMessage($"{label} باید ریال صحیح و نامنفی باشد (بدون اعشار).");
}
