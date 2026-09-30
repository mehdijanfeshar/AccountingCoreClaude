using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetBankStatementBookCandidates;

public sealed class GetBankStatementBookCandidatesQueryValidator : AbstractValidator<GetBankStatementBookCandidatesQuery>
{
    public GetBankStatementBookCandidatesQueryValidator()
    {
        RuleFor(x => x.StatementId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
    }
}
