using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.ActivateFsTemplateVersion;

public sealed class ActivateFsTemplateVersionCommandValidator : AbstractValidator<ActivateFsTemplateVersionCommand>
{
    public ActivateFsTemplateVersionCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.EffectiveFromYear)
            .InclusiveBetween(1300, 1600)
            .WithMessage("سال شروع اعتبار باید سال مالی شمسی چهاررقمی باشد.");
    }
}
