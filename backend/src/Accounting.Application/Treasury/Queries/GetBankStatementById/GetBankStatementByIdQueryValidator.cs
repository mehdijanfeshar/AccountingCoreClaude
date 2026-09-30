using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetBankStatementById;

public sealed class GetBankStatementByIdQueryValidator : AbstractValidator<GetBankStatementByIdQuery>
{
    public GetBankStatementByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
