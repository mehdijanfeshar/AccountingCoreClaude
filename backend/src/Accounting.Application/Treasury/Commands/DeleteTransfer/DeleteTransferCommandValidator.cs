using FluentValidation;

namespace Accounting.Application.Treasury.Commands.DeleteTransfer;

public sealed class DeleteTransferCommandValidator : AbstractValidator<DeleteTransferCommand>
{
    public DeleteTransferCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
