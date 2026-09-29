using FluentValidation;

namespace Accounting.Application.Treasury.Commands.CreateTreasuryRole;

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
