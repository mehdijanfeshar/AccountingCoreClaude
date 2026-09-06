using FluentValidation;

namespace Accounting.Application.PayReciveHeads.Commands.UpdatePayReciveHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c> and mirroring <c>CreatePayReciveHeadCommandValidator</c> rule for rule.
/// Per the recorded "Legacy fully replaces the rich model" architecture decision, accounting
/// invariants must NOT be re-created here — including the reference project's duplicate
/// document-number guard, which has no UNIQUE constraint behind it in our schema.
/// </summary>
public sealed class UpdatePayReciveHeadCommandValidator : AbstractValidator<UpdatePayReciveHeadCommand>
{
    public UpdatePayReciveHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.PayReciveCode)
            .NotEmpty()
            .MaximumLength(5);

        RuleFor(x => x.PayReciveDate)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.PayReciveDescription)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);
    }
}
