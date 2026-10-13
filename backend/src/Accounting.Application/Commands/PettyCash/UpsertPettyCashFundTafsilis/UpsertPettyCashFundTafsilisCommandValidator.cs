using Accounting.Application.Commands.PettyCash.Common;
using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.UpsertPettyCashFundTafsilis;

public sealed class UpsertPettyCashFundTafsilisCommandValidator : AbstractValidator<UpsertPettyCashFundTafsilisCommand>
{
    public UpsertPettyCashFundTafsilisCommandValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);

        RuleForEach(x => x.Tafsilis).SetValidator(new PettyCashFundTafsiliLinkInputValidator());
    }
}
