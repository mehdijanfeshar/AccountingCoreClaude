using FluentValidation;

namespace Accounting.Application.Commands.Treasury.UnmatchBankStatementLine;

public sealed class UnmatchBankStatementLineCommandValidator : AbstractValidator<UnmatchBankStatementLineCommand>
{
    public UnmatchBankStatementLineCommandValidator()
    {
        RuleFor(x => x.StatementId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
