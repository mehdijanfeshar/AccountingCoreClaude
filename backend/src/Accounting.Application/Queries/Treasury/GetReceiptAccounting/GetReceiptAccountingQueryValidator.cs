using FluentValidation;

namespace Accounting.Application.Queries.Treasury.GetReceiptAccounting;

public sealed class GetReceiptAccountingQueryValidator : AbstractValidator<GetReceiptAccountingQuery>
{
    public GetReceiptAccountingQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
