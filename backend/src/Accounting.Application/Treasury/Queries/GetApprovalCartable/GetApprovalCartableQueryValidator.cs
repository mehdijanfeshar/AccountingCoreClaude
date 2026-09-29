using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetApprovalCartable;

public sealed class GetApprovalCartableQueryValidator : AbstractValidator<GetApprovalCartableQuery>
{
    public GetApprovalCartableQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
