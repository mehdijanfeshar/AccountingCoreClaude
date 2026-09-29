using FluentValidation;

namespace Accounting.Application.Treasury.Commands.DeleteReceipt;

public sealed class DeleteReceiptCommandValidator : AbstractValidator<DeleteReceiptCommand>
{
    public DeleteReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
