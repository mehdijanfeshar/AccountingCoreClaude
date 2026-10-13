using FluentValidation;

namespace Accounting.Application.Commands.Treasury.DeletePaymentRequest;

public sealed class DeletePaymentRequestCommandValidator : AbstractValidator<DeletePaymentRequestCommand>
{
    public DeletePaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
