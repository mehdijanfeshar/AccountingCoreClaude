using FluentValidation;

namespace Accounting.Application.Commands.Treasury.SubmitPaymentRequest;

public sealed class SubmitPaymentRequestCommandValidator : AbstractValidator<SubmitPaymentRequestCommand>
{
    public SubmitPaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
