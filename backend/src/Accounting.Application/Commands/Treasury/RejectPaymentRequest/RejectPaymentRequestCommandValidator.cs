using FluentValidation;

namespace Accounting.Application.Commands.Treasury.RejectPaymentRequest;

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
