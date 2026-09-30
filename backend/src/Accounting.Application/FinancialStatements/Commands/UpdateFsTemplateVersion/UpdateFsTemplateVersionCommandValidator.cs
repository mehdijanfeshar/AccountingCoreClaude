using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.UpdateFsTemplateVersion;

public sealed class UpdateFsTemplateVersionCommandValidator : AbstractValidator<UpdateFsTemplateVersionCommand>
{
    public UpdateFsTemplateVersionCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
