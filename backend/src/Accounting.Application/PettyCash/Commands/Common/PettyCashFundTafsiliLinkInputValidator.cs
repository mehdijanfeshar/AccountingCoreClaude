using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashFundTafsiliLinkInputValidator : AbstractValidator<PettyCashFundTafsiliLinkInput>
{
    public PettyCashFundTafsiliLinkInputValidator()
    {
        RuleFor(x => x.TafsiliId).NotEmpty();
        RuleFor(x => x.LevelId).NotEmpty();
    }
}
