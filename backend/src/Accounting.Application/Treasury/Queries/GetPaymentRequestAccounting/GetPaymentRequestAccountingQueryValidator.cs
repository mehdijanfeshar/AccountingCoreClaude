using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetPaymentRequestAccounting;

public sealed class GetPaymentRequestAccountingQueryValidator : AbstractValidator<GetPaymentRequestAccountingQuery>
{
    public GetPaymentRequestAccountingQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
