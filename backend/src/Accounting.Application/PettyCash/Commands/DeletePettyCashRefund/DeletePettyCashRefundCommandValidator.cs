using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashRefund;

public sealed class DeletePettyCashRefundCommandValidator : AbstractValidator<DeletePettyCashRefundCommand>
{
    public DeletePettyCashRefundCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
