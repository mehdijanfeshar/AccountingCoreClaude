using FluentValidation;

namespace Accounting.Application.AccountCodeInterfaces.Commands.CreateAccountCodeInterface;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants were deliberately discarded and must NOT be re-created here.
/// </summary>
public sealed class CreateAccountCodeInterfaceCommandValidator : AbstractValidator<CreateAccountCodeInterfaceCommand>
{
    public CreateAccountCodeInterfaceCommandValidator()
    {
        RuleFor(x => x.AccountCodeId)
            .NotEqual(Guid.Empty);
    }
}
