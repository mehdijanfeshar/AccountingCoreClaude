using FluentValidation;

namespace Accounting.Application.Treasury.Commands.CancelReceipt;

public sealed class CancelReceiptCommandValidator : AbstractValidator<CancelReceiptCommand>
{
    public CancelReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
