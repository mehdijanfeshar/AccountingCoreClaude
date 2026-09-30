using Accounting.Application.FinancialStatements.Engine;
using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.CreateFsTemplate;

public sealed class CreateFsTemplateCommandValidator : AbstractValidator<CreateFsTemplateCommand>
{
    public CreateFsTemplateCommandValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.StatementType).IsInEnum();

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Must(FsText.IsValidTemplateCode)
            .WithMessage("کد قالب باید از بخش‌های حرف/رقم/زیرخط لاتین جداشده با نقطه باشد (مثل PENSION.NET_ASSETS).");

        RuleFor(x => x.TitleFa).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TitleEn).MaximumLength(200);
        RuleFor(x => x.OrderNo).InclusiveBetween(0, 99999);

        Common.FsTemplateRules.AddNoteLinkRules(this, x => x.NoteParentTemplateCode, x => x.NoteParentRowCode, x => x.NoteTotalRowCode);
        RuleFor(x => x.NoteParentRowCode)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.NoteParentTemplateCode))
            .WithMessage("برای یادداشت وصل به صورت، کد ردیف صورت هم لازم است.");
    }
}
