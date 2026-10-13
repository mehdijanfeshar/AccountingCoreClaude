using FluentValidation;

namespace Accounting.Application.Queries.Treasury.GetBankAccountBalance;

public sealed class GetBankAccountBalanceQueryValidator : AbstractValidator<GetBankAccountBalanceQuery>
{
    public GetBankAccountBalanceQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
