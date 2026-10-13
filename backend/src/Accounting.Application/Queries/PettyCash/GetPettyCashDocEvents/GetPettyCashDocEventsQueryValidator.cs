using FluentValidation;

namespace Accounting.Application.Queries.PettyCash.GetPettyCashDocEvents;

public sealed class GetPettyCashDocEventsQueryValidator : AbstractValidator<GetPettyCashDocEventsQuery>
{
    public GetPettyCashDocEventsQueryValidator()
    {
        RuleFor(x => x.ExpenseDocId).NotEmpty();
    }
}
