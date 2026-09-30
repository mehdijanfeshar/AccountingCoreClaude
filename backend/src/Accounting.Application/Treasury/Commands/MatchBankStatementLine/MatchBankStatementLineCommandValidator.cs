using FluentValidation;

namespace Accounting.Application.Treasury.Commands.MatchBankStatementLine;

public sealed class MatchBankStatementLineCommandValidator : AbstractValidator<MatchBankStatementLineCommand>
{
    public MatchBankStatementLineCommandValidator()
    {
        RuleFor(x => x.StatementId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.VoucherDetailId).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
