using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.ReverseVoucher;

public sealed class ReverseVoucherCommandValidator : AbstractValidator<ReverseVoucherCommand>
{
    public ReverseVoucherCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
