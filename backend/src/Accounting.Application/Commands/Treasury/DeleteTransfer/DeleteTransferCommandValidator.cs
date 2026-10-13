using FluentValidation;

namespace Accounting.Application.Commands.Treasury.DeleteTransfer;

public sealed class DeleteTransferCommandValidator : AbstractValidator<DeleteTransferCommand>
{
    public DeleteTransferCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
