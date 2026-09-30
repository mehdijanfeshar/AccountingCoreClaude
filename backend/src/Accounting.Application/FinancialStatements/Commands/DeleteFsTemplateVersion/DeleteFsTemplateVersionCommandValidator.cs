using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsTemplateVersion;

public sealed class DeleteFsTemplateVersionCommandValidator : AbstractValidator<DeleteFsTemplateVersionCommand>
{
    public DeleteFsTemplateVersionCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
