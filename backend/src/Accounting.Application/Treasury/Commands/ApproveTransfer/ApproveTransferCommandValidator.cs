using FluentValidation;

namespace Accounting.Application.Treasury.Commands.ApproveTransfer;

public sealed class ApproveTransferCommandValidator : AbstractValidator<ApproveTransferCommand>
{
    public ApproveTransferCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.BankReference)
            .NotEmpty()
            .MaximumLength(100);
    }
}
