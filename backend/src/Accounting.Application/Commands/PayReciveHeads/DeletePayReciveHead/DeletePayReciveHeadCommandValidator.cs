using Accounting.Application.PayReciveHeads;
using FluentValidation;

namespace Accounting.Application.Commands.PayReciveHeads.DeletePayReciveHead;

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
