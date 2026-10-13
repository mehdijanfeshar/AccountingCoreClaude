using FluentValidation;

namespace Accounting.Application.Commands.Treasury.ResumePaymentRequest;

public sealed class ResumePaymentRequestCommandValidator : AbstractValidator<ResumePaymentRequestCommand>
{
    public ResumePaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
