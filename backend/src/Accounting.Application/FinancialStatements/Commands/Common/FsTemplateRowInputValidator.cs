using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.ValueObjects;
using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.Common;

/// <summary>
/// قواعد تک‌ردیفی (بدون نگاه به ردیف‌های دیگر): کد، طول فیلدها، فیلدهای لازم هر نوع، و <b>نحو</b>
/// انتخاب‌گر/فرمول. ارجاع به ردیف‌های دیگر و دور فقط در <see cref="FsTemplateChecker"/> (validate/activate)
/// بررسی می‌شود، چون ردیف‌ها یکی‌یکی اضافه می‌شوند.
/// </summary>
public sealed class FsTemplateRowInputValidator : AbstractValidator<FsTemplateRowInput>
{
    public FsTemplateRowInputValidator()
    {
        RuleFor(x => x.Code)
            .Must(FsText.IsValidRowCode)
            .WithMessage("کد ردیف باید با حرف لاتین شروع شود، حداکثر ۲۰ نویسهٔ حرف/رقم/زیرخط باشد و نام تابع نباشد.");

        RuleFor(x => x.ParentCode)
            .Must(FsText.IsValidRowCode).WithMessage("کد ردیف والد نامعتبر است.")
            .NotEqual(x => x.Code).WithMessage("ردیف نمی‌تواند والد خودش باشد.")
            .When(x => x.ParentCode is not null);

        RuleFor(x => x.RowType).IsInEnum();
        RuleFor(x => x.NormalBalance).IsInEnum().When(x => x.NormalBalance.HasValue);
        RuleFor(x => x.ValueType).IsInEnum().When(x => x.ValueType.HasValue);

        RuleFor(x => x.TitleFa).MaximumLength(500);
        RuleFor(x => x.TitleEn).MaximumLength(500);
        RuleFor(x => x.NoteRef).MaximumLength(20);
        RuleFor(x => x.Selector).MaximumLength(1000);
        RuleFor(x => x.Formula).MaximumLength(1000);
        RuleFor(x => x.OrderNo).InclusiveBetween(0, 99999).When(x => x.OrderNo.HasValue);

        RuleFor(x => x.Format!.Indent).InclusiveBetween(0, 5).When(x => x.Format is not null);
        RuleFor(x => x.Format!.TopBorder).IsInEnum().When(x => x.Format is not null);
        RuleFor(x => x.Format!.BottomBorder).IsInEnum().When(x => x.Format is not null);

        When(x => x.RowType == FsRowType.Account, () =>
        {
            RuleFor(x => x.Selector)
                .NotEmpty().WithMessage("ردیف حساب به انتخاب‌گر حساب نیاز دارد.")
                .Custom((s, ctx) =>
                {
                    if (!string.IsNullOrWhiteSpace(s) && !AccountSelector.TryParse(s, out _, out var err))
                    {
                        ctx.AddFailure(err!);
                    }
                });
            RuleFor(x => x.ValueType).NotNull().WithMessage("نوع مقدار ردیف حساب لازم است.");
            RuleFor(x => x.NormalBalance).NotNull().WithMessage("ماهیت ردیف لازم است.");
        });

        When(x => x.RowType == FsRowType.Formula, () =>
        {
            RuleFor(x => x.Formula)
                .NotEmpty().WithMessage("ردیف فرمول به فرمول نیاز دارد.")
                .Custom((f, ctx) =>
                {
                    if (!string.IsNullOrWhiteSpace(f) && !FsFormula.TryParse(f, out _, out var err))
                    {
                        ctx.AddFailure(err!);
                    }
                });
            RuleFor(x => x.NormalBalance).NotNull().WithMessage("ماهیت ردیف لازم است.");
        });

        When(x => x.RowType == FsRowType.External, () =>
        {
            RuleFor(x => x.NormalBalance).NotNull().WithMessage("ماهیت ردیف لازم است.");
        });
    }
}
