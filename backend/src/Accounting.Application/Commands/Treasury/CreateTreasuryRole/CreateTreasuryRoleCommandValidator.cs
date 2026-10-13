using FluentValidation;

namespace Accounting.Application.Commands.Treasury.CreateTreasuryRole;

public sealed class CreateTreasuryRoleCommandValidator : AbstractValidator<CreateTreasuryRoleCommand>
{
    public CreateTreasuryRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.UserName).MaximumLength(200);

        RuleFor(x => x.Role).IsInEnum();

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
