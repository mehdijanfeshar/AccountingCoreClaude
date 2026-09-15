using FluentValidation;

namespace Accounting.Application.ChequeTypes.Commands.CreateChequeType;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. No range rule is applied to any <c>byte?</c> layout field beyond what
/// the CLR type (0-255) already enforces — the Oracle precision (3 or 4 digits) is looser than
/// <c>byte</c>'s range, so <c>byte</c> is already the tighter, safe constraint.
/// <see cref="CreateChequeTypeCommand.ChequeImage"/> carries no size-limit rule — see the
/// command's XML doc for why that is a deliberately unmade decision, not an oversight.
///
/// The <c>RuleFor(x => x.VahedCode)</c> below is a deliberate second belt, not dead code: by the
/// time this validator runs, <c>VahedScopeBehavior</c> (registered ahead of
/// <c>ValidationBehavior</c> — see <c>DependencyInjection.cs</c>) has already overwritten
/// <see cref="CreateChequeTypeCommand.VahedCode"/> with the server-assigned value, so this rule
/// now validates that value rather than anything the caller supplied.
/// </summary>
public sealed class CreateChequeTypeCommandValidator : AbstractValidator<CreateChequeTypeCommand>
{
    public CreateChequeTypeCommandValidator()
    {
        RuleFor(x => x.ChequeTypeTitle)
            .MaximumLength(25);

        RuleFor(x => x.ChequeAdateFont)
            .MaximumLength(200);

        RuleFor(x => x.ChequeNdateFont)
            .MaximumLength(200);

        RuleFor(x => x.ChequeAamountFont)
            .MaximumLength(200);

        RuleFor(x => x.ChequeLamountFont)
            .MaximumLength(200);

        RuleFor(x => x.ChequeNamountFont)
            .MaximumLength(200);

        RuleFor(x => x.ChequeDescribe1Font)
            .MaximumLength(200);

        RuleFor(x => x.ChequeDescribe2Font)
            .MaximumLength(200);

        RuleFor(x => x.ChequeBreaklineFont)
            .MaximumLength(200);

        RuleFor(x => x.PrinterType)
            .MaximumLength(100);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
