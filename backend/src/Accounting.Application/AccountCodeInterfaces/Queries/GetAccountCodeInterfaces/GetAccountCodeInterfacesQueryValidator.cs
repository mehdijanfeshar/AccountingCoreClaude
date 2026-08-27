using FluentValidation;

namespace Accounting.Application.AccountCodeInterfaces.Queries.GetAccountCodeInterfaces;

/// <summary>
/// Surface-level (syntactic) pagination validation only.
/// </summary>
public sealed class GetAccountCodeInterfacesQueryValidator : AbstractValidator<GetAccountCodeInterfacesQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <c>PageNumber</c>. Chosen as <c>int.MaxValue / MaxPageSize</c> so that,
    /// for any allowed <c>PageSize</c>, the repository's <c>(pageNumber - 1) * pageSize</c>
    /// computation in <c>Skip(...)</c> can never overflow <see cref="int"/>.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetAccountCodeInterfacesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
