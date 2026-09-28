using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashRefund;

public sealed class CreatePettyCashRefundCommandValidator : AbstractValidator<CreatePettyCashRefundCommand>
{
    public CreatePettyCashRefundCommandValidator()
    {
        RuleFor(x => x.FundId).NotEmpty();

        RuleFor(x => x.Amount).GreaterThan(0);

        RuleFor(x => x.RefundDate)
            .NotEmpty()
            .Matches("^[0-9]{8}$").WithMessage("تاریخ استرداد باید به شکل YYYYMMDD باشد.");

        RuleFor(x => x.Reason).MaximumLength(500);

        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
