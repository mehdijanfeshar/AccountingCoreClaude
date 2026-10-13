using Accounting.Application.FinancialStatements;
using FluentValidation;

namespace Accounting.Application.Commands.FinancialStatements.DeleteFsRun;

public sealed class DeleteFsRunCommandValidator : AbstractValidator<DeleteFsRunCommand>
{
    public DeleteFsRunCommandValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
