using FluentValidation;

namespace Accounting.Application.ChequeTypes.Commands.UpdateChequeType;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Same rationale as <c>CreateChequeTypeCommandValidator</c> for why no
/// range rule is applied to the <c>byte?</c> layout fields and no size-limit rule is applied to
/// <c>ChequeImage</c>.
/// </summary>
public sealed class UpdateChequeTypeCommandValidator : AbstractValidator<UpdateChequeTypeCommand>
{
    public UpdateChequeTypeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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
