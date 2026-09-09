using FluentValidation;

namespace Accounting.Application.ChequeTypes.Commands.UpdateChequeType;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Same rationale as <c>CreateChequeTypeCommandValidator</c> for why no
/// range rule is applied to the <c>byte?</c> layout fields and no size-limit rule is applied to
/// <c>ChequeImage</c>.
///
/// ⚠️ Unlike the Create-side validator, the <c>RuleFor(x => x.VahedCode)</c> rule below currently
/// does NOT execute at runtime: <c>ValidationBehavior</c>'s <c>where TRequest : IRequest&lt;TResponse&gt;</c>
/// constraint is never satisfied for this void (<c>: IRequest</c>) command in MediatR 14.2.0, so
/// the DI container silently skips this validator for every <c>Update</c>/<c>Delete</c> request
/// project-wide — see <c>docs/open-decisions.md</c> and <c>VahedScopeBehavior.cs</c> (which
/// deliberately avoids the same constraint for this exact reason). This is safe at runtime only
/// because <c>VahedScopeBehavior</c> unconditionally overwrites <c>VahedCode</c> before the
/// handler runs, not because of this rule.
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
