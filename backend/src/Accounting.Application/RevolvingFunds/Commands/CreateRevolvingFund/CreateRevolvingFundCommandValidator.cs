using FluentValidation;

namespace Accounting.Application.RevolvingFunds.Commands.CreateRevolvingFund;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here. <see cref="CreateRevolvingFundCommand.AccountCodeId"/>
/// carries no rule beyond nullability since it is genuinely optional (<c>ACCOUNTCODE_ID</c> is
/// nullable) — the real DB constraint (<c>FK_ACCOUNTCODE_REVOLVING</c>) is enforced centrally.
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
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);
    }
}
