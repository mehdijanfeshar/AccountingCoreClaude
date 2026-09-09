using FluentValidation;

namespace Accounting.Application.CheckBooks.Commands.CreateCheckBook;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here. <see cref="CreateCheckBookCommand.CheckTypeId"/>
/// carries no rule beyond nullability since it is genuinely optional (<c>CHECKTYPE_ID</c> is
/// nullable).
///
/// The <c>RuleFor(x => x.VahedCode)</c> below is a deliberate second belt, not dead code: by the
/// time this validator runs, <c>VahedScopeBehavior</c> (registered ahead of
/// <c>ValidationBehavior</c> — see <c>DependencyInjection.cs</c>) has already overwritten
/// <see cref="CreateCheckBookCommand.VahedCode"/> with the server-assigned value, so this rule now
/// validates that value rather than anything the caller supplied.
/// </summary>
public sealed class CreateCheckBookCommandValidator : AbstractValidator<CreateCheckBookCommand>
{
    public CreateCheckBookCommandValidator()
    {
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
