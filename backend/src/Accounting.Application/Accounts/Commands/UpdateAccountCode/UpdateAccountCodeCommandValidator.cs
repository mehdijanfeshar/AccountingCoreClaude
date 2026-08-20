using FluentValidation;

namespace Accounting.Application.Accounts.Commands.UpdateAccountCode;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants (hierarchy rules, required-detail, etc.)
/// were deliberately discarded and must NOT be re-created here.
/// </summary>
public sealed class UpdateAccountCodeCommandValidator : AbstractValidator<UpdateAccountCodeCommand>
{
    public UpdateAccountCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => x.ParentId != x.Id)
            .WithMessage("ParentId cannot be the row's own Id — this would create a self-referencing cycle in the coding hierarchy.")
            .WithName("ParentId");

        RuleFor(x => x.AccCode)
            .NotEmpty()
            .MaximumLength(6);

        RuleFor(x => x.AccCodeName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.MoInforClose)
            .MaximumLength(6);
    }
}
