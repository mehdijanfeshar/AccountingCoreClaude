using FluentValidation;

namespace Accounting.Application.Commands.Treasury.CloseBankStatement;

public sealed class CloseBankStatementCommandValidator : AbstractValidator<CloseBankStatementCommand>
{
    public CloseBankStatementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
