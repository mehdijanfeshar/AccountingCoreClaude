using FluentValidation;

namespace Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackLists;

/// <summary>
/// Surface-level (syntactic) pagination and filter validation only.
/// </summary>
public sealed class GetWhiteAndBlackListsQueryValidator : AbstractValidator<GetWhiteAndBlackListsQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <see cref="GetWhiteAndBlackListsQuery.PageNumber"/>. Chosen as
    /// <c>int.MaxValue / MaxPageSize</c> so that, for any allowed <c>PageSize</c> (up to
    /// <see cref="MaxPageSize"/>), the repository's <c>(pageNumber - 1) * pageSize</c>
    /// computation in <c>Skip(...)</c> can never overflow <see cref="int"/>. Do not remove
    /// without re-checking that overflow guarantee.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetWhiteAndBlackListsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);

        RuleFor(x => x.AccountCodeId)
            .NotEqual(Guid.Empty)
            .When(x => x.AccountCodeId is not null);

        RuleFor(x => x.VahedTypeId)
            .NotEqual(Guid.Empty)
            .When(x => x.VahedTypeId is not null);

        RuleFor(x => x.State)
            .IsInEnum()
            .When(x => x.State is not null);

        // The four date bounds are compared as strings against zero-padded YYYYMMDD Jalali text
        // (see the query's XML doc). A value that is not exactly 8 digits would compare
        // lexicographically against a different-width string and silently return a wrong page,
        // so it is rejected rather than accepted and mis-applied.
        RuleFor(x => x.FromAuthorizedDate)
            .Matches(LegacyJalaliDatePattern)
            .When(x => !string.IsNullOrEmpty(x.FromAuthorizedDate));

        RuleFor(x => x.ToAuthorizedDate)
            .Matches(LegacyJalaliDatePattern)
            .When(x => !string.IsNullOrEmpty(x.ToAuthorizedDate));

        RuleFor(x => x.FromLimitationDate)
            .Matches(LegacyJalaliDatePattern)
            .When(x => !string.IsNullOrEmpty(x.FromLimitationDate));

        RuleFor(x => x.ToLimitationDate)
            .Matches(LegacyJalaliDatePattern)
            .When(x => !string.IsNullOrEmpty(x.ToLimitationDate));
    }

    /// <summary>
    /// Exactly eight digits — the <c>YYYYMMDD</c> Jalali encoding every date column on this table
    /// uses. Deliberately not a calendar check: the Legacy column is <c>VARCHAR2(8)</c> and
    /// already holds values this project did not write.
    /// </summary>
    internal const string LegacyJalaliDatePattern = @"^\d{8}$";
}
