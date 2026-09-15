using FluentValidation;

namespace Accounting.Application.RevolvingFunds.Commands.Common;

/// <summary>
/// Surface-level (syntactic) validation only, mirroring
/// <c>BankAccounts.Commands.Common.BankAccountTafsiliLinkInputValidator</c> — including what it
/// deliberately does not do: it does not verify that the تفصیلی is permitted for the معین, nor
/// that every active level has one. Both remain open items, not re-created here.
///
/// Both fields get <c>NotEmpty</c> because the columns are non-nullable <see cref="Guid"/>:
/// <see cref="Guid.Empty"/> would be persisted as a real, meaningless key.
/// </summary>
public sealed class RevolvingFundTafsiliLinkInputValidator : AbstractValidator<RevolvingFundTafsiliLinkInput>
{
    public RevolvingFundTafsiliLinkInputValidator()
    {
        RuleFor(x => x.TafsiliId)
            .NotEmpty();

        RuleFor(x => x.LevelId)
            .NotEmpty();
    }
}
