using FluentValidation;

namespace Accounting.Application.Commands.Treasury.AutoMatchBankStatement;

public sealed class AutoMatchBankStatementCommandValidator : AbstractValidator<AutoMatchBankStatementCommand>
{
    public AutoMatchBankStatementCommandValidator()
    {
        RuleFor(x => x.StatementId).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
