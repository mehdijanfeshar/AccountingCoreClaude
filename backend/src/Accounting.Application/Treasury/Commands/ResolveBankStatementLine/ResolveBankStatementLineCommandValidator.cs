using FluentValidation;

namespace Accounting.Application.Treasury.Commands.ResolveBankStatementLine;

/// <summary>Surface-level only — per-<c>Type</c> business rules (direction, receipt state/amount/
/// bank match, note-required-for-Ignored) live in <c>IBankStatementLineResolutionService</c>.</summary>
public sealed class ResolveBankStatementLineCommandValidator : AbstractValidator<ResolveBankStatementLineCommand>
{
    public ResolveBankStatementLineCommandValidator()
    {
        RuleFor(x => x.StatementId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(1000);
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
