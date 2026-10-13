using FluentValidation;

namespace Accounting.Application.Commands.Treasury.DeleteReceipt;

public sealed class DeleteReceiptCommandValidator : AbstractValidator<DeleteReceiptCommand>
{
    public DeleteReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
