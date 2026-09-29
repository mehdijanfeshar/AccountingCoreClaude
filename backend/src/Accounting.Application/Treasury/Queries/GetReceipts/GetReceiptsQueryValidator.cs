using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetReceipts;

public sealed class GetReceiptsQueryValidator : AbstractValidator<GetReceiptsQuery>
{
    public GetReceiptsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.State).IsInEnum().When(x => x.State.HasValue);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
