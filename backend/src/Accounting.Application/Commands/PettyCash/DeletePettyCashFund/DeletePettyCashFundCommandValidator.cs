using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.DeletePettyCashFund;

public sealed class DeletePettyCashFundCommandValidator : AbstractValidator<DeletePettyCashFundCommand>
{
    public DeletePettyCashFundCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
