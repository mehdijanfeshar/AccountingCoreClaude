using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsTemplate;

public sealed class DeleteFsTemplateCommandValidator : AbstractValidator<DeleteFsTemplateCommand>
{
    public DeleteFsTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
