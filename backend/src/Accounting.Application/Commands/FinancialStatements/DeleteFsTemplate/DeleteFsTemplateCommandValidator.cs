using Accounting.Application.FinancialStatements;
using FluentValidation;

namespace Accounting.Application.Commands.FinancialStatements.DeleteFsTemplate;

public sealed class DeleteFsTemplateCommandValidator : AbstractValidator<DeleteFsTemplateCommand>
{
    public DeleteFsTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
