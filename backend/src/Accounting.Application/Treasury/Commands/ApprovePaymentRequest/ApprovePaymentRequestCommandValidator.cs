using FluentValidation;

namespace Accounting.Application.Treasury.Commands.ApprovePaymentRequest;

public sealed class ApprovePaymentRequestCommandValidator : AbstractValidator<ApprovePaymentRequestCommand>
{
    public ApprovePaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
