using FluentValidation;

namespace Accounting.Application.Treasury.Commands.DeleteBankStatement;

public sealed class DeleteBankStatementCommandValidator : AbstractValidator<DeleteBankStatementCommand>
{
    public DeleteBankStatementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
