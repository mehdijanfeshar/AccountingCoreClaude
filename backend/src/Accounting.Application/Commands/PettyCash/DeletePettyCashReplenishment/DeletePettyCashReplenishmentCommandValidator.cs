using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.DeletePettyCashReplenishment;

public sealed class DeletePettyCashReplenishmentCommandValidator : AbstractValidator<DeletePettyCashReplenishmentCommand>
{
    public DeletePettyCashReplenishmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
