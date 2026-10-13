using FluentValidation;

namespace Accounting.Application.Commands.Treasury.BulkApprovePaymentRequests;

public sealed class BulkApprovePaymentRequestsCommandValidator : AbstractValidator<BulkApprovePaymentRequestsCommand>
{
    public BulkApprovePaymentRequestsCommandValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty()
            .WithMessage("حداقل یک شناسه باید ارسال شود.");
    }
}
