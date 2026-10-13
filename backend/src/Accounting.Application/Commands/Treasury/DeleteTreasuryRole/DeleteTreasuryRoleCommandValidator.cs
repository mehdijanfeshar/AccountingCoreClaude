using FluentValidation;

namespace Accounting.Application.Commands.Treasury.DeleteTreasuryRole;

public sealed class DeleteTreasuryRoleCommandValidator : AbstractValidator<DeleteTreasuryRoleCommand>
{
    public DeleteTreasuryRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
