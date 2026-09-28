using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashFund;

public sealed class DeletePettyCashFundCommandValidator : AbstractValidator<DeletePettyCashFundCommand>
{
    public DeletePettyCashFundCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
