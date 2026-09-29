using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetTransferAccounting;

public sealed class GetTransferAccountingQueryValidator : AbstractValidator<GetTransferAccountingQuery>
{
    public GetTransferAccountingQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
