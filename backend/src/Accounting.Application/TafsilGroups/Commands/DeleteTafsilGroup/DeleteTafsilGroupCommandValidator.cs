using FluentValidation;

namespace Accounting.Application.TafsilGroups.Commands.DeleteTafsilGroup;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteTafsilGroupCommandValidator : AbstractValidator<DeleteTafsilGroupCommand>
{
    public DeleteTafsilGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
