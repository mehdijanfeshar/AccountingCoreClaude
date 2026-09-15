using FluentValidation;

namespace Accounting.Application.AccountCodeInterfaces.Commands.UpdateAccountCodeInterface;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class UpdateAccountCodeInterfaceCommandValidator : AbstractValidator<UpdateAccountCodeInterfaceCommand>
{
    public UpdateAccountCodeInterfaceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.AccountCodeId)
            .NotEqual(Guid.Empty);
    }
}
