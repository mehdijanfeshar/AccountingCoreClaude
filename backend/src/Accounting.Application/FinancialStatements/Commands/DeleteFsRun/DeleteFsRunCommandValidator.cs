using FluentValidation;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsRun;

public sealed class DeleteFsRunCommandValidator : AbstractValidator<DeleteFsRunCommand>
{
    public DeleteFsRunCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
