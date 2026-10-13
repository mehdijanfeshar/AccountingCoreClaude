using FluentValidation;

namespace Accounting.Application.Commands.Treasury.SubmitTransfer;

public sealed class SubmitTransferCommandValidator : AbstractValidator<SubmitTransferCommand>
{
    public SubmitTransferCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
