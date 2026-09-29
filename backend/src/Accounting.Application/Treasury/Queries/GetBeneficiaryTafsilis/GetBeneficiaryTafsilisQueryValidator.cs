using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetBeneficiaryTafsilis;

/// <summary>Surface-level (syntactic) validation only.</summary>
public sealed class GetBeneficiaryTafsilisQueryValidator : AbstractValidator<GetBeneficiaryTafsilisQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Same overflow-guard rationale as <c>GetTafsiliLevelItemsQueryValidator.MaxPageNumber</c> —
    /// keeps the repository's <c>(pageNumber - 1) * pageSize</c> from overflowing <see cref="int"/>.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetBeneficiaryTafsilisQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);

        RuleFor(x => x.PageNumber).InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}
