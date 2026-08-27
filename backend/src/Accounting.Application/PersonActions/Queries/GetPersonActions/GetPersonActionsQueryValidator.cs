using FluentValidation;

namespace Accounting.Application.PersonActions.Queries.GetPersonActions;

/// <summary>
/// Surface-level (syntactic) pagination validation only.
/// </summary>
public sealed class GetPersonActionsQueryValidator : AbstractValidator<GetPersonActionsQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <c>PageNumber</c>, chosen so that <c>(pageNumber - 1) * pageSize</c> in
    /// <c>Skip(...)</c> can never overflow <see cref="int"/>.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetPersonActionsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
