using FluentValidation;

namespace Accounting.Application.Rabets.Commands.UpdateRabet;

/// <summary>
/// Surface-level validation only — the sole rule is on the route-bound <c>Id</c>.
/// <see cref="UpdateRabetCommand.RabetTypeId"/>/<see cref="UpdateRabetCommand.AccountCodeId"/>
/// carry no further rules, for the same reason documented on
/// <c>CreateRabetCommandValidator</c>.
/// </summary>
public sealed class UpdateRabetCommandValidator : AbstractValidator<UpdateRabetCommand>
{
    public UpdateRabetCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
