using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetTransfers;

public sealed class GetTransfersQueryValidator : AbstractValidator<GetTransfersQuery>
{
    public GetTransfersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.State).IsInEnum().When(x => x.State.HasValue);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
