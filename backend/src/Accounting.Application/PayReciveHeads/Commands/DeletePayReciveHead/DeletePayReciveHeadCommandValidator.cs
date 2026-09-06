using FluentValidation;

namespace Accounting.Application.PayReciveHeads.Commands.DeletePayReciveHead;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeletePayReciveHeadCommandValidator : AbstractValidator<DeletePayReciveHeadCommand>
{
    public DeletePayReciveHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
