using FluentValidation;

namespace Accounting.Application.Queries.WorkShops.GetWorkShopById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetWorkShopByIdQueryValidator : AbstractValidator<GetWorkShopByIdQuery>
{
    public GetWorkShopByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
