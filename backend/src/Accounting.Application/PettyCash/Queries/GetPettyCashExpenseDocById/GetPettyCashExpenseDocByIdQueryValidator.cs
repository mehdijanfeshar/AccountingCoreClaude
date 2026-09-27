using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashExpenseDocById;

public sealed class GetPettyCashExpenseDocByIdQueryValidator : AbstractValidator<GetPettyCashExpenseDocByIdQuery>
{
    public GetPettyCashExpenseDocByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
