using FluentValidation;

namespace Accounting.Application.Queries.Treasury.GetPaymentRequestAccounting;

public sealed class GetPaymentRequestAccountingQueryValidator : AbstractValidator<GetPaymentRequestAccountingQuery>
{
    public GetPaymentRequestAccountingQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
