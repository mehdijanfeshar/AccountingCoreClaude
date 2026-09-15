using FluentValidation;

namespace Accounting.Application.AccountExceptions.Queries.GetAccountExceptions;

/// <summary>
/// Surface-level (syntactic) pagination validation only.
/// </summary>
public sealed class GetAccountExceptionsQueryValidator : AbstractValidator<GetAccountExceptionsQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Upper bound for <c>PageNumber</c>, chosen so that <c>(pageNumber - 1) * pageSize</c> in
    /// <c>Skip(...)</c> can never overflow <see cref="int"/>.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetAccountExceptionsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
