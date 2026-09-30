using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.UpdateFsTemplate;

public sealed class UpdateFsTemplateCommandValidator : AbstractValidator<UpdateFsTemplateCommand>
{
    public UpdateFsTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
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
