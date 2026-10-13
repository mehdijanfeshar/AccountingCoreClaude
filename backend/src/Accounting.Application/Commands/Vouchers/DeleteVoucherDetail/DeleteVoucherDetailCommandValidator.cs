using FluentValidation;

namespace Accounting.Application.Commands.Vouchers.DeleteVoucherDetail;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteVoucherDetailCommandValidator : AbstractValidator<DeleteVoucherDetailCommand>
{
    public DeleteVoucherDetailCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
