using FluentValidation;

namespace Accounting.Application.Treasury.Commands.DeleteBankStatementLine;

public sealed class DeleteBankStatementLineCommandValidator : AbstractValidator<DeleteBankStatementLineCommand>
{
    public DeleteBankStatementLineCommandValidator()
    {
        RuleFor(x => x.StatementId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
