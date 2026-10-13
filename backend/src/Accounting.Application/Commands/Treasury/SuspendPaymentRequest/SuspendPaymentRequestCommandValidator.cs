using FluentValidation;

namespace Accounting.Application.Commands.Treasury.SuspendPaymentRequest;

public sealed class SuspendPaymentRequestCommandValidator : AbstractValidator<SuspendPaymentRequestCommand>
{
    public SuspendPaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("دلیل تعلیق الزامی است.")
            .MaximumLength(1000);
    }
}
