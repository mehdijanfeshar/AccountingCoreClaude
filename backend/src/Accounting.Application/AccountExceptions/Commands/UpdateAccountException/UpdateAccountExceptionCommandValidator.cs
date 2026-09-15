using FluentValidation;

namespace Accounting.Application.AccountExceptions.Commands.UpdateAccountException;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class UpdateAccountExceptionCommandValidator : AbstractValidator<UpdateAccountExceptionCommand>
{
    public UpdateAccountExceptionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.AccountCoeId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.VahedTypeId)
            .NotEqual(Guid.Empty);
    }
}
