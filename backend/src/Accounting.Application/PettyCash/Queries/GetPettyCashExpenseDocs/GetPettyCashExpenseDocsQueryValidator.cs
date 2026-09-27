using Accounting.Domain.ValueObjects;
using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashExpenseDocs;

public sealed class GetPettyCashExpenseDocsQueryValidator : AbstractValidator<GetPettyCashExpenseDocsQuery>
{
    public const int MaxPageSize = 200;

    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetPettyCashExpenseDocsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, MaxPageNumber);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(200);

        RuleFor(x => x.State)
            .IsInEnum()
            .When(x => x.State.HasValue);

        RuleFor(x => x.States)
            .Must(states => states!.All(s => Enum.IsDefined(typeof(PettyCashDocState), s)))
            .WithMessage("'States' contains an out-of-range value.")
            .When(x => x.States is { Count: > 0 });
    }
}
