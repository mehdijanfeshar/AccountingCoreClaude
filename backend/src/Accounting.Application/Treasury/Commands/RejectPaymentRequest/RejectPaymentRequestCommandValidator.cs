using FluentValidation;

namespace Accounting.Application.Treasury.Commands.RejectPaymentRequest;

public sealed class RejectPaymentRequestCommandValidator : AbstractValidator<RejectPaymentRequestCommand>
{
    public RejectPaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("دلیل رد الزامی است.")
            .MaximumLength(1000);
    }
}
