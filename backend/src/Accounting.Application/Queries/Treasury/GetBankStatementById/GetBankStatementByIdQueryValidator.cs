using FluentValidation;

namespace Accounting.Application.Queries.Treasury.GetBankStatementById;

public sealed class GetBankStatementByIdQueryValidator : AbstractValidator<GetBankStatementByIdQuery>
{
    public GetBankStatementByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
