using FluentValidation;

namespace Accounting.Application.Commands.Treasury.ApproveTransfer;

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
