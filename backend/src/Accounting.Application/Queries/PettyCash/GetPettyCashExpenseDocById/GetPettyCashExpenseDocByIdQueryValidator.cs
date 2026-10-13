using FluentValidation;

namespace Accounting.Application.Queries.PettyCash.GetPettyCashExpenseDocById;

public sealed class GetPettyCashExpenseDocByIdQueryValidator : AbstractValidator<GetPettyCashExpenseDocByIdQuery>
{
    public GetPettyCashExpenseDocByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
