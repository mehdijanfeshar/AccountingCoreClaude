using FluentValidation;

namespace Accounting.Application.Treasury.Commands.Common;

public sealed class PaymentRequestTafsiliLinkInputValidator : AbstractValidator<PaymentRequestTafsiliLinkInput>
{
    public PaymentRequestTafsiliLinkInputValidator()
    {
        RuleFor(x => x.TafsiliId).NotEmpty();
        RuleFor(x => x.LevelId).NotEmpty();
    }
}
