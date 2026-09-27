using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundSetting;

public sealed class GetPettyCashFundSettingQueryValidator : AbstractValidator<GetPettyCashFundSettingQuery>
{
    public GetPettyCashFundSettingQueryValidator()
    {
        RuleFor(x => x.FundId).NotEmpty();
    }
}
