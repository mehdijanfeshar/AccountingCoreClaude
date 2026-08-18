using FluentValidation;

namespace Accounting.Application.Vouchers.Queries.GetVoucherHeads;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c> (<c>YEAR</c> and <c>VAHEDCODE</c> are both max 4 chars).
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

        RuleFor(x => x.VahedCode)
            .MaximumLength(4);
    }
}
