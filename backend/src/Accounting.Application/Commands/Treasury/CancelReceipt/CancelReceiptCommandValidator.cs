using FluentValidation;

namespace Accounting.Application.Commands.Treasury.CancelReceipt;

public sealed class CancelReceiptCommandValidator : AbstractValidator<CancelReceiptCommand>
{
    public CancelReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
