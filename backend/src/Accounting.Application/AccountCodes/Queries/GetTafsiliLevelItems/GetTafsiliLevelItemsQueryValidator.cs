using FluentValidation;

namespace Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;

/// <summary>
/// Surface-level (syntactic) validation only. <see cref="MaxPageSize"/>/<see cref="MaxPageNumber"/>
/// copy <c>Accounting.Application.Expenses.Queries.GetExpenses.GetExpensesQueryValidator</c>
/// verbatim, including its overflow-guard rationale.
/// </summary>
public sealed class GetTafsiliLevelItemsQueryValidator : AbstractValidator<GetTafsiliLevelItemsQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <see cref="GetTafsiliLevelItemsQuery.PageNumber"/>. Chosen as
    /// <c>int.MaxValue / MaxPageSize</c> so that, for any allowed <c>PageSize</c> (up to
    /// <see cref="MaxPageSize"/>), the repository's <c>(pageNumber - 1) * pageSize</c>
    /// computation in <c>Skip(...)</c> can never overflow <see cref="int"/>. Do not remove
    /// without re-checking that overflow guarantee.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetTafsiliLevelItemsQueryValidator()
    {
        RuleFor(x => x.AccountCodeId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.LevelId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Search)
            // Matches TB_TAFSILI.TAFSILI_NAME's mapped length (LegacyDbContext:
            // entity.Property(e => e.TAFSILI_NAME).HasMaxLength(200)) — a search term longer than
            // the longest possible match is never meaningful. Blank/whitespace-only IS allowed
            // and is treated as "no filter" by the repository, not rejected here.
            .MaximumLength(200);

        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
