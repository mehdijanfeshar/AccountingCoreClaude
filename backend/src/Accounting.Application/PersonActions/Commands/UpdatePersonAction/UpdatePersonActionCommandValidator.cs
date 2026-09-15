using FluentValidation;

namespace Accounting.Application.PersonActions.Commands.UpdatePersonAction;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class UpdatePersonActionCommandValidator : AbstractValidator<UpdatePersonActionCommand>
{
    public UpdatePersonActionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.UserName)
            .MaximumLength(30);

        RuleFor(x => x.UserId)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.FromDate)
            .MaximumLength(8);

        RuleFor(x => x.ToDate)
            .MaximumLength(8);

        RuleFor(x => x.VahedCode)
            .MaximumLength(4);
    }
}
