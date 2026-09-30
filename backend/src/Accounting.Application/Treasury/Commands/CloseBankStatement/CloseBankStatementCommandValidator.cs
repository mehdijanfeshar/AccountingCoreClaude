using FluentValidation;

namespace Accounting.Application.Treasury.Commands.CloseBankStatement;

public sealed class CloseBankStatementCommandValidator : AbstractValidator<CloseBankStatementCommand>
{
    public CloseBankStatementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
