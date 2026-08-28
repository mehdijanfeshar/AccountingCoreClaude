using FluentValidation;

namespace Accounting.Application.WorkShops.Commands.DeleteWorkShop;

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
