using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.ApprovePettyCashReplenishment;

public sealed class ApprovePettyCashReplenishmentCommandValidator : AbstractValidator<ApprovePettyCashReplenishmentCommand>
{
    public ApprovePettyCashReplenishmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
