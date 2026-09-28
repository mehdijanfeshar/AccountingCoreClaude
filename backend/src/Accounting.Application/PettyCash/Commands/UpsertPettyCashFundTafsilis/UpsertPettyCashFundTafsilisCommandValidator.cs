using Accounting.Application.PettyCash.Commands.Common;
using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.UpsertPettyCashFundTafsilis;

public sealed class UpsertPettyCashFundTafsilisCommandValidator : AbstractValidator<UpsertPettyCashFundTafsilisCommand>
{
    public UpsertPettyCashFundTafsilisCommandValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);

        RuleForEach(x => x.Tafsilis).SetValidator(new PettyCashFundTafsiliLinkInputValidator());
    }
}
