using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.RecordPettyCashReplenishmentPayment;

public sealed class RecordPettyCashReplenishmentPaymentCommandValidator : AbstractValidator<RecordPettyCashReplenishmentPaymentCommand>
{
    public RecordPettyCashReplenishmentPaymentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
