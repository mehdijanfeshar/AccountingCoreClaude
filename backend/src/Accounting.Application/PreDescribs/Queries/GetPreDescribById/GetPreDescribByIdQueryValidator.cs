using FluentValidation;

namespace Accounting.Application.PreDescribs.Queries.GetPreDescribById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetPreDescribByIdQueryValidator : AbstractValidator<GetPreDescribByIdQuery>
{
    public GetPreDescribByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
