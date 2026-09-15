using FluentValidation;

namespace Accounting.Application.WhiteLists.Commands.DeleteWhiteList;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteWhiteListCommandValidator : AbstractValidator<DeleteWhiteListCommand>
{
    public DeleteWhiteListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
