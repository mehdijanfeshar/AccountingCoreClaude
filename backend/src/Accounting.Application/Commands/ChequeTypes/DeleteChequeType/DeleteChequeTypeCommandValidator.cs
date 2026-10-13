using FluentValidation;

namespace Accounting.Application.Commands.ChequeTypes.DeleteChequeType;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteChequeTypeCommandValidator : AbstractValidator<DeleteChequeTypeCommand>
{
    public DeleteChequeTypeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
