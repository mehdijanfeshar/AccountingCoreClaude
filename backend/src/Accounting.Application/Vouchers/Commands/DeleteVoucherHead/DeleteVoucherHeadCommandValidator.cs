using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.DeleteVoucherHead;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteVoucherHeadCommandValidator : AbstractValidator<DeleteVoucherHeadCommand>
{
    public DeleteVoucherHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
