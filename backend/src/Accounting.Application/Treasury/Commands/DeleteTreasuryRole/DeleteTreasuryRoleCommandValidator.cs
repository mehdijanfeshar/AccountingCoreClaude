using FluentValidation;

namespace Accounting.Application.Treasury.Commands.DeleteTreasuryRole;

public sealed class DeleteTreasuryRoleCommandValidator : AbstractValidator<DeleteTreasuryRoleCommand>
{
    public DeleteTreasuryRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
