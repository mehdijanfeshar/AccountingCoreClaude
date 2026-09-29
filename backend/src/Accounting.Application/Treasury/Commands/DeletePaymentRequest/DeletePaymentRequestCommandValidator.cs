using FluentValidation;

namespace Accounting.Application.Treasury.Commands.DeletePaymentRequest;

public sealed class DeletePaymentRequestCommandValidator : AbstractValidator<DeletePaymentRequestCommand>
{
    public DeletePaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
