using FluentValidation;

namespace Accounting.Application.Commands.Vouchers.ReverseVoucher;

public sealed class ReverseVoucherCommandValidator : AbstractValidator<ReverseVoucherCommand>
{
    public ReverseVoucherCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
