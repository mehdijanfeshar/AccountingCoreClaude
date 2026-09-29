using FluentValidation;

namespace Accounting.Application.Treasury.Commands.ReturnPaymentRequest;

public sealed class ReturnPaymentRequestCommandValidator : AbstractValidator<ReturnPaymentRequestCommand>
{
    public ReturnPaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("دلیل برگشت الزامی است.")
            .MaximumLength(1000);
    }
}
