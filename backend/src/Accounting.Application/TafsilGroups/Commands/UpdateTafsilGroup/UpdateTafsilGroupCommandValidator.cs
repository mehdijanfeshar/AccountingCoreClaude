using FluentValidation;

namespace Accounting.Application.TafsilGroups.Commands.UpdateTafsilGroup;

/// <summary>
/// Surface-level validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
///
/// <c>.IsInEnum()</c> on <see cref="UpdateTafsilGroupCommand.PersonType"/> mirrors
/// <c>CreateTafsilGroupCommandValidator</c> (phase 27 batch 1).
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

        RuleFor(x => x.PersonType)
            .IsInEnum()
            .When(x => x.PersonType.HasValue);
    }
}
