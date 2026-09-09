using FluentValidation;

namespace Accounting.Application.PayReciveHeads.Commands.UpdatePayReciveHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c> and mirroring <c>CreatePayReciveHeadCommandValidator</c> rule for rule.
/// Per the recorded "Legacy fully replaces the rich model" architecture decision, accounting
/// invariants must NOT be re-created here — including the reference project's duplicate
/// document-number guard, which has no UNIQUE constraint behind it in our schema.
///
/// ⚠️ The <c>RuleFor(x => x.VahedCode)</c> below currently does NOT execute at runtime:
/// <c>ValidationBehavior</c>'s <c>where TRequest : IRequest&lt;TResponse&gt;</c> constraint is
/// never satisfied for this void (<c>: IRequest</c>) command in MediatR 14.2.0, so the DI
/// container silently skips this validator for every <c>Update</c>/<c>Delete</c> request
/// project-wide — see <c>docs/open-decisions.md</c> and <c>VahedScopeBehavior.cs</c> (which
/// deliberately avoids the same constraint for this exact reason).
/// <see cref="UpdatePayReciveHeadCommand.VahedCode"/> is still safe at runtime only because
/// <c>VahedScopeBehavior</c> unconditionally overwrites it before the handler runs, not because
/// of this rule.
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
