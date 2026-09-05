using FluentValidation;

namespace Accounting.Application.RevolvingFunds.Commands.DeleteRevolvingFund;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteRevolvingFundCommandValidator : AbstractValidator<DeleteRevolvingFundCommand>
{
    public DeleteRevolvingFundCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
