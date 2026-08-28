using FluentValidation;

namespace Accounting.Application.WorkShops.Queries.GetWorkShopById;

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
