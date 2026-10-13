using FluentValidation;

namespace Accounting.Application.Commands.Treasury.ReturnTransfer;

public sealed class ReturnTransferCommandValidator : AbstractValidator<ReturnTransferCommand>
{
    public ReturnTransferCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
