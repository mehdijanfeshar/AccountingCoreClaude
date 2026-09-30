using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.CreateFsTemplateVersion;

public sealed class CreateFsTemplateVersionCommandValidator : AbstractValidator<CreateFsTemplateVersionCommand>
{
    public CreateFsTemplateVersionCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEqual(Guid.Empty);
        RuleFor(x => x.SourceVersionId).NotEqual(Guid.Empty).When(x => x.SourceVersionId.HasValue);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
