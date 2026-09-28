using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.RejectPettyCashReplenishment;

public sealed class RejectPettyCashReplenishmentCommandValidator : AbstractValidator<RejectPettyCashReplenishmentCommand>
{
    public RejectPettyCashReplenishmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000);
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
