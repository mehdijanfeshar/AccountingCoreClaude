using Accounting.Application.FinancialStatements;
using FluentValidation;

namespace Accounting.Application.Commands.FinancialStatements.DeleteFsTemplateVersion;

public sealed class DeleteFsTemplateVersionCommandValidator : AbstractValidator<DeleteFsTemplateVersionCommand>
{
    public DeleteFsTemplateVersionCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
