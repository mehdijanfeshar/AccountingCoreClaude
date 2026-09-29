using FluentValidation;

namespace Accounting.Application.Treasury.Commands.ResumePaymentRequest;

public sealed class ResumePaymentRequestCommandValidator : AbstractValidator<ResumePaymentRequestCommand>
{
    public ResumePaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
