using FluentValidation;

namespace Accounting.Application.FinancialStatements.Queries.GetFsRuns;

public sealed class GetFsRunsQueryValidator : AbstractValidator<GetFsRunsQuery>
{
    public GetFsRunsQueryValidator()
    {
        RuleFor(x => x.Year).Matches("^[0-9]{4}$").When(x => !string.IsNullOrWhiteSpace(x.Year));
    }
}
