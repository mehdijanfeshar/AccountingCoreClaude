using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetCustomerTafsilis;

/// <summary>Surface-level (syntactic) validation only.</summary>
public sealed class GetCustomerTafsilisQueryValidator : AbstractValidator<GetCustomerTafsilisQuery>
{
    public const int MaxPageSize = 200;

    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetCustomerTafsilisQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);

        RuleFor(x => x.PageNumber).InclusiveBetween(1, MaxPageNumber);

        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}
