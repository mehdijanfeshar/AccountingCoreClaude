using Accounting.Application.FinancialStatements;
using FluentValidation;

namespace Accounting.Application.Commands.FinancialStatements.UpdateFsTemplateVersion;

public sealed class UpdateFsTemplateVersionCommandValidator : AbstractValidator<UpdateFsTemplateVersionCommand>
{
    public UpdateFsTemplateVersionCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
