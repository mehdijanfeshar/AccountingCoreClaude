using FluentValidation;

namespace Accounting.Application.TmpVoucherHeads.Commands.DeleteTmpVoucherHead;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteTmpVoucherHeadCommandValidator : AbstractValidator<DeleteTmpVoucherHeadCommand>
{
    public DeleteTmpVoucherHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
