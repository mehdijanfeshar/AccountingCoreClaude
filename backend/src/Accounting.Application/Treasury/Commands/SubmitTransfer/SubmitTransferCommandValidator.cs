using FluentValidation;

namespace Accounting.Application.Treasury.Commands.SubmitTransfer;

public sealed class SubmitTransferCommandValidator : AbstractValidator<SubmitTransferCommand>
{
    public SubmitTransferCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
