using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.Common;

public sealed class PettyCashFundTafsiliLinkInputValidator : AbstractValidator<PettyCashFundTafsiliLinkInput>
{
    public PettyCashFundTafsiliLinkInputValidator()
    {
        RuleFor(x => x.TafsiliId).NotEmpty();
        RuleFor(x => x.LevelId).NotEmpty();
    }
}
