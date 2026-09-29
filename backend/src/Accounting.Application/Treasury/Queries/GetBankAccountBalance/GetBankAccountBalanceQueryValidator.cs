using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetBankAccountBalance;

public sealed class GetBankAccountBalanceQueryValidator : AbstractValidator<GetBankAccountBalanceQuery>
{
    public GetBankAccountBalanceQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
