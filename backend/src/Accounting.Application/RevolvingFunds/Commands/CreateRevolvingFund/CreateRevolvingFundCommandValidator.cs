using FluentValidation;

namespace Accounting.Application.RevolvingFunds.Commands.CreateRevolvingFund;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here. <see cref="CreateRevolvingFundCommand.AccountCodeId"/>
/// carries no rule beyond nullability since it is genuinely optional (<c>ACCOUNTCODE_ID</c> is
/// nullable) — the real DB constraint (<c>FK_ACCOUNTCODE_REVOLVING</c>) is enforced centrally.
///
/// The <c>RuleFor(x => x.VahedCode)</c> below is a deliberate second belt, not dead code: by the
/// time this validator runs, <c>VahedScopeBehavior</c> (registered ahead of
/// <c>ValidationBehavior</c> — see <c>DependencyInjection.cs</c>) has already overwritten
/// <see cref="CreateRevolvingFundCommand.VahedCode"/> with the server-assigned value, so this
/// rule now validates that value rather than anything the caller supplied. <c>NotEmpty</c> is
/// applied even though the underlying <c>VAHEDCODE</c> column is nullable at the Legacy schema
/// level — the caller can never leave this field blank any more, since it is never
/// client-supplied.
/// </summary>
public sealed class CreateRevolvingFundCommandValidator : AbstractValidator<CreateRevolvingFundCommand>
{
    public CreateRevolvingFundCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(2);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(100);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);
    }
}
