using FluentValidation;

namespace Accounting.Application.Treasury.Commands.SubmitPaymentRequest;

public sealed class SubmitPaymentRequestCommandValidator : AbstractValidator<SubmitPaymentRequestCommand>
{
    public SubmitPaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
