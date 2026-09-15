using FluentValidation;

namespace Accounting.Application.TafsilGroups.Commands.UpdateTafsilGroup;

/// <summary>
/// Surface-level validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class UpdateTafsilGroupCommandValidator : AbstractValidator<UpdateTafsilGroupCommand>
{
    public UpdateTafsilGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.TafsilGroupCode)
            .NotEmpty()
            .MaximumLength(3);

        RuleFor(x => x.TafsilGroupName)
            .NotEmpty()
            .MaximumLength(200);
    }
}
