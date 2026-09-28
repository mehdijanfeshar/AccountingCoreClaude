using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashReplenishments;

public sealed class GetPettyCashReplenishmentsQueryValidator : AbstractValidator<GetPettyCashReplenishmentsQuery>
{
    public const int MaxPageSize = 200;

    public GetPettyCashReplenishmentsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.State).IsInEnum().When(x => x.State.HasValue);
    }
}
