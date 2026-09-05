using FluentValidation;

namespace Accounting.Application.CheckBooks.Commands.UpdateCheckBook;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
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
