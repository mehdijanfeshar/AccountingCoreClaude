using FluentValidation;

namespace Accounting.Application.AccountCodeInterfaces.Commands.DeleteAccountCodeInterface;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteAccountCodeInterfaceCommandValidator : AbstractValidator<DeleteAccountCodeInterfaceCommand>
{
    public DeleteAccountCodeInterfaceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
