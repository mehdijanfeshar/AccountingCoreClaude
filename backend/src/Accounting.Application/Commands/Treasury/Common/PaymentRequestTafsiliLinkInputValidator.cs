using FluentValidation;

namespace Accounting.Application.Commands.Treasury.Common;

public sealed class PaymentRequestTafsiliLinkInputValidator : AbstractValidator<PaymentRequestTafsiliLinkInput>
{
    public PaymentRequestTafsiliLinkInputValidator()
    {
        RuleFor(x => x.TafsiliId).NotEmpty();
        RuleFor(x => x.LevelId).NotEmpty();
    }
}
