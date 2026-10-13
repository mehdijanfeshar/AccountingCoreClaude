using FluentValidation;

namespace Accounting.Application.Queries.Treasury.GetTransferAccounting;

public sealed class GetTransferAccountingQueryValidator : AbstractValidator<GetTransferAccountingQuery>
{
    public GetTransferAccountingQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
