using FluentValidation;

namespace Accounting.Application.Commands.WhiteAndBlackLists.DeleteWhiteAndBlackList;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteWhiteAndBlackListCommandValidator : AbstractValidator<DeleteWhiteAndBlackListCommand>
{
    public DeleteWhiteAndBlackListCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
