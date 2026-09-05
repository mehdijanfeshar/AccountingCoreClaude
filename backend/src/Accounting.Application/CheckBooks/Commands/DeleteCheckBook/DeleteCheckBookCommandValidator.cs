using FluentValidation;

namespace Accounting.Application.CheckBooks.Commands.DeleteCheckBook;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteCheckBookCommandValidator : AbstractValidator<DeleteCheckBookCommand>
{
    public DeleteCheckBookCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
