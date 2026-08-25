using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants (debit==credit balance, post-immutability,
/// required-detail, etc.) were deliberately discarded and must NOT be re-created here.
/// </summary>
public sealed class CreateVoucherHeadCommandValidator : AbstractValidator<CreateVoucherHeadCommand>
{
    public CreateVoucherHeadCommandValidator()
    {
        RuleFor(x => x.DocNum)
            .NotEmpty()
            .MaximumLength(6);

        RuleFor(x => x.DateDoc)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.HeadDesc)
            .MaximumLength(250);

        RuleFor(x => x.Apendix)
            .MaximumLength(800);

        RuleFor(x => x.SndVahedCode)
            .MaximumLength(4);

        RuleFor(x => x.AttachFileName)
            .MaximumLength(100);

        RuleFor(x => x.AtfNum)
            .MaximumLength(15);

        // No-op when InitialDetails is null (composite-create is opt-in) — the explicit
        // `.When` guard, rather than relying on RuleForEach's own null-tolerance, documents
        // that behaviour at the call site per the acceptance criteria for this rule.
        RuleForEach(x => x.InitialDetails)
            .SetValidator(new CreateVoucherHeadDetailInputValidator())
            .When(x => x.InitialDetails is not null);
    }
}
