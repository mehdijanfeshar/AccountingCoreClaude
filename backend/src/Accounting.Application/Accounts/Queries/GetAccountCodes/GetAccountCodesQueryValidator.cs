using FluentValidation;

namespace Accounting.Application.Accounts.Queries.GetAccountCodes;

/// <summary>
/// Surface-level (syntactic) pagination validation only.
/// </summary>
public sealed class GetAccountCodesQueryValidator : AbstractValidator<GetAccountCodesQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <see cref="Accounting.Application.Accounts.Queries.GetAccountCodes.GetAccountCodesQuery.PageNumber"/>.
    /// Chosen as <c>int.MaxValue / MaxPageSize</c> so that, for any allowed <c>PageSize</c>
    /// (up to <see cref="MaxPageSize"/>), the repository's <c>(pageNumber - 1) * pageSize</c>
    /// computation in <c>Skip(...)</c> can never overflow <see cref="int"/>. Do not remove
    /// without re-checking that overflow guarantee.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetAccountCodesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
