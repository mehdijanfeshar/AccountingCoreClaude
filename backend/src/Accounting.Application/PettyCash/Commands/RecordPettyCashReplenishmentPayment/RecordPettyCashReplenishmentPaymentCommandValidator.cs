using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.RecordPettyCashReplenishmentPayment;

public sealed class RecordPettyCashReplenishmentPaymentCommandValidator : AbstractValidator<RecordPettyCashReplenishmentPaymentCommand>
{
    public RecordPettyCashReplenishmentPaymentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
