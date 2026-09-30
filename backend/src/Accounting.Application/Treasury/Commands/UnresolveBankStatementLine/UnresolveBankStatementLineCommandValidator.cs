using FluentValidation;

namespace Accounting.Application.Treasury.Commands.UnresolveBankStatementLine;

public sealed class UnresolveBankStatementLineCommandValidator : AbstractValidator<UnresolveBankStatementLineCommand>
{
    public UnresolveBankStatementLineCommandValidator()
    {
        RuleFor(x => x.StatementId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
