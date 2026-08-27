using FluentValidation;

namespace Accounting.Application.WhiteLists.Queries.GetWhiteListById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetWhiteListByIdQueryValidator : AbstractValidator<GetWhiteListByIdQuery>
{
    public GetWhiteListByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
