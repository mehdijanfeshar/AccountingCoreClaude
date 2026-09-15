using FluentValidation;

namespace Accounting.Application.Tafsilis.Queries.GetTafsilis;

/// <summary>
/// Surface-level (syntactic) pagination validation only.
/// </summary>
public sealed class GetTafsilisQueryValidator : AbstractValidator<GetTafsilisQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <see cref="GetTafsilisQuery.PageNumber"/>. Chosen as
    /// <c>int.MaxValue / MaxPageSize</c> so that, for any allowed <c>PageSize</c> (up to
    /// <see cref="MaxPageSize"/>), the repository's <c>(pageNumber - 1) * pageSize</c>
    /// computation in <c>Skip(...)</c> can never overflow <see cref="int"/>.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetTafsilisQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
