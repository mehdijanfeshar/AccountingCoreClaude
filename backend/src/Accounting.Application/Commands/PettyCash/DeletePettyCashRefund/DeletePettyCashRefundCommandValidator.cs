using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.DeletePettyCashRefund;

public sealed class DeletePettyCashRefundCommandValidator : AbstractValidator<DeletePettyCashRefundCommand>
{
    public DeletePettyCashRefundCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
