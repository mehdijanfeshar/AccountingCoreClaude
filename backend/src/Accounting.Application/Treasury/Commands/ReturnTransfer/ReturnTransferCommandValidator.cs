using FluentValidation;

namespace Accounting.Application.Treasury.Commands.ReturnTransfer;

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
