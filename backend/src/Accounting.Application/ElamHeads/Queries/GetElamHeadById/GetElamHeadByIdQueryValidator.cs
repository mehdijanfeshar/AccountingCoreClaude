using FluentValidation;

namespace Accounting.Application.ElamHeads.Queries.GetElamHeadById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetElamHeadByIdQueryValidator : AbstractValidator<GetElamHeadByIdQuery>
{
    public GetElamHeadByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
