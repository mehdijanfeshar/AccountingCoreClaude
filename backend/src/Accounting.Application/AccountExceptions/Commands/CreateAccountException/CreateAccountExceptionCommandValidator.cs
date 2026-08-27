using FluentValidation;

namespace Accounting.Application.AccountExceptions.Commands.CreateAccountException;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class CreateAccountExceptionCommandValidator : AbstractValidator<CreateAccountExceptionCommand>
{
    public CreateAccountExceptionCommandValidator()
    {
        RuleFor(x => x.AccountCoeId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.VahedTypeId)
            .NotEqual(Guid.Empty);
    }
}
