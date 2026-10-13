using FluentValidation;

namespace Accounting.Application.Commands.Treasury.RejectTransfer;

public sealed class RejectTransferCommandValidator : AbstractValidator<RejectTransferCommand>
{
    public RejectTransferCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
