using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.SubmitPettyCashReplenishment;

public sealed class SubmitPettyCashReplenishmentCommandValidator : AbstractValidator<SubmitPettyCashReplenishmentCommand>
{
    public SubmitPettyCashReplenishmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
