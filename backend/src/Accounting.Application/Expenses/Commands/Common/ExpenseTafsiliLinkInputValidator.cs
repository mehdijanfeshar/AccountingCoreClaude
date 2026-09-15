using FluentValidation;

namespace Accounting.Application.Expenses.Commands.Common;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints for
/// <c>TB_EXPENCE_LINK_TAFSILI</c> in <c>LegacyDbContext</c>. Mirrors
/// <c>VoucherDetailTafsiliLinkInputValidator</c>, including what it deliberately does NOT do:
/// per the recorded "Legacy fully replaces the rich model" decision, it does not verify that the
/// تفصیلی is <i>permitted</i> for the expense's معین (the
/// <c>TB_ACCOUNTCODE → TB_ACCOUNT_LINK_TAFSILGROUP → TB_TAFSIL_LINK_TAFSILGROUP → TB_TAFSILI</c>
/// chain), and does not require a تفصیلی for every active level. The UI drives both from
/// <c>GetTafsiliLevels</c>/<c>GetTafsiliLevelItems</c>, but the API accepts whatever it is sent.
///
/// Both fields get <c>NotEmpty</c> because <c>TAFSILI_ID</c> and <c>LEVEL_ID</c> are non-nullable
/// <see cref="Guid"/> columns: <see cref="Guid.Empty"/> is not an "absent FK" there, it is a real
/// meaningless key that would be persisted.
/// </summary>
public sealed class ExpenseTafsiliLinkInputValidator : AbstractValidator<ExpenseTafsiliLinkInput>
{
    public ExpenseTafsiliLinkInputValidator()
    {
        RuleFor(x => x.TafsiliId)
            .NotEmpty();

        RuleFor(x => x.LevelId)
            .NotEmpty();
    }
}
