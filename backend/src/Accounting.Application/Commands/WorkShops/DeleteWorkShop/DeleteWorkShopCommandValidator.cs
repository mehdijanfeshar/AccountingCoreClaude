using FluentValidation;

namespace Accounting.Application.Commands.WorkShops.DeleteWorkShop;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteWorkShopCommandValidator : AbstractValidator<DeleteWorkShopCommand>
{
    public DeleteWorkShopCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
