using FluentValidation;

namespace Accounting.Application.Queries.PettyCash.GetPettyCashReplenishmentPreview;

public sealed class GetPettyCashReplenishmentPreviewQueryValidator : AbstractValidator<GetPettyCashReplenishmentPreviewQuery>
{
    public GetPettyCashReplenishmentPreviewQueryValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);
    }
}
