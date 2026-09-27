using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashDocEvents;

public sealed class GetPettyCashDocEventsQueryValidator : AbstractValidator<GetPettyCashDocEventsQuery>
{
    public GetPettyCashDocEventsQueryValidator()
    {
        RuleFor(x => x.ExpenseDocId).NotEmpty();
    }
}
