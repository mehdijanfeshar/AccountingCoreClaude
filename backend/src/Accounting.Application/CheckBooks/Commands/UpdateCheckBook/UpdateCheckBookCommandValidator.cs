using FluentValidation;

namespace Accounting.Application.CheckBooks.Commands.UpdateCheckBook;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
///
/// ⚠️ The <c>RuleFor(x => x.VahedCode)</c> below currently does NOT execute at runtime:
/// <c>ValidationBehavior</c>'s <c>where TRequest : IRequest&lt;TResponse&gt;</c> constraint is
/// never satisfied for this void (<c>: IRequest</c>) command in MediatR 14.2.0, so the DI
/// container silently skips this validator for every <c>Update</c>/<c>Delete</c> request
/// project-wide — see <c>docs/open-decisions.md</c> and <c>VahedScopeBehavior.cs</c> (which
/// deliberately avoids the same constraint for this exact reason).
/// <see cref="UpdateCheckBookCommand.VahedCode"/> is still safe at runtime only because
/// <c>VahedScopeBehavior</c> unconditionally overwrites it before the handler runs, not because
/// of this rule (kept for when the pipeline bug is fixed, and for direct unit testing of this
/// validator).
/// </summary>
public sealed class UpdateCheckBookCommandValidator : AbstractValidator<UpdateCheckBookCommand>
{
    public UpdateCheckBookCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.AccountId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.CheckBookTitle)
            .MaximumLength(100);

        RuleFor(x => x.CheckBookDate)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.FromCheckNumber)
            .NotEmpty()
            .MaximumLength(14);

        RuleFor(x => x.ToCheckNumber)
            .NotEmpty()
            .MaximumLength(14);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Serial)
            .MaximumLength(20);
    }
}
