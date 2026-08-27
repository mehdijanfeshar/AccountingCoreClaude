using FluentValidation;

namespace Accounting.Application.PreDescribs.Commands.CreatePreDescrib;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants were deliberately discarded and must NOT be
/// re-created here.
/// </summary>
public sealed class CreatePreDescribCommandValidator : AbstractValidator<CreatePreDescribCommand>
{
    public CreatePreDescribCommandValidator()
    {
        RuleFor(x => x.Descrip)
            .MaximumLength(200);

        RuleFor(x => x.VahedCode)
            .MaximumLength(4);
    }
}
