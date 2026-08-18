using FluentValidation;

namespace Accounting.Application.Accounts.Commands.CreateAccountCode;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants (hierarchy rules, required-detail, etc.)
/// were deliberately discarded and must NOT be re-created here.
/// </summary>
public sealed class CreateAccountCodeCommandValidator : AbstractValidator<CreateAccountCodeCommand>
{
    public CreateAccountCodeCommandValidator()
    {
        RuleFor(x => x.AccCode)
            .NotEmpty()
            .MaximumLength(6);

        RuleFor(x => x.AccCodeName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.AddUserId)
            .MaximumLength(10);

        RuleFor(x => x.MoInforClose)
            .MaximumLength(6);
    }
}
