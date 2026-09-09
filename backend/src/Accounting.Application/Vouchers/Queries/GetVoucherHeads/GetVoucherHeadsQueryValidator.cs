using FluentValidation;

namespace Accounting.Application.Vouchers.Queries.GetVoucherHeads;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraint on
/// <c>YEAR</c> (max 4 chars). No rule for <c>VahedCode</c> here — mirrors
/// <c>GetWorkShopsQueryValidator</c>: it is never caller input (server-assigned by
/// <c>VahedScopeBehavior</c>, always a valid value by the time this validator runs), so there is
/// nothing meaningful for a syntactic validator to check.
/// </summary>
public sealed class GetVoucherHeadsQueryValidator : AbstractValidator<GetVoucherHeadsQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <see cref="Accounting.Application.Vouchers.Queries.GetVoucherHeads.GetVoucherHeadsQuery.PageNumber"/>.
    /// Chosen as <c>int.MaxValue / MaxPageSize</c> so that, for any allowed <c>PageSize</c>
    /// (up to <see cref="MaxPageSize"/>), the repository's <c>(pageNumber - 1) * pageSize</c>
    /// computation in <c>Skip(...)</c> can never overflow <see cref="int"/>. Do not remove
    /// without re-checking that overflow guarantee.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetVoucherHeadsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);

        RuleFor(x => x.Year)
            .MaximumLength(4);
    }
}
