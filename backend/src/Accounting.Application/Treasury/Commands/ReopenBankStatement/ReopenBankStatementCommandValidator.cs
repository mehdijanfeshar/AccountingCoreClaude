using FluentValidation;

namespace Accounting.Application.Treasury.Commands.ReopenBankStatement;

public sealed class ReopenBankStatementCommandValidator : AbstractValidator<ReopenBankStatementCommand>
{
    public ReopenBankStatementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
