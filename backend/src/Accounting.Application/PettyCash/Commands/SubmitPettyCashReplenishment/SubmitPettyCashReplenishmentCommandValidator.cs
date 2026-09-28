using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.SubmitPettyCashReplenishment;

public sealed class SubmitPettyCashReplenishmentCommandValidator : AbstractValidator<SubmitPettyCashReplenishmentCommand>
{
    public SubmitPettyCashReplenishmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
