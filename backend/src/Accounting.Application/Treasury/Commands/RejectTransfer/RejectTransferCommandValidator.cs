using FluentValidation;

namespace Accounting.Application.Treasury.Commands.RejectTransfer;

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
