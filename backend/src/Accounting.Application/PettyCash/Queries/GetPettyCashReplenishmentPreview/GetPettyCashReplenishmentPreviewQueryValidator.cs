using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashReplenishmentPreview;

public sealed class GetPettyCashReplenishmentPreviewQueryValidator : AbstractValidator<GetPettyCashReplenishmentPreviewQuery>
{
    public GetPettyCashReplenishmentPreviewQueryValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);
    }
}
