using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetBankStatements;

public sealed class GetBankStatementsQueryValidator : AbstractValidator<GetBankStatementsQuery>
{
    public GetBankStatementsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.State).IsInEnum().When(x => x.State.HasValue);
    }
}
