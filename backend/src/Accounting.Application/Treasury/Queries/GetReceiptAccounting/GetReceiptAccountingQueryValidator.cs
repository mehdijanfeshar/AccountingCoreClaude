using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetReceiptAccounting;

public sealed class GetReceiptAccountingQueryValidator : AbstractValidator<GetReceiptAccountingQuery>
{
    public GetReceiptAccountingQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
