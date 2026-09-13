using FluentValidation;

namespace Accounting.Application.Tafsilis.Queries.GetTafsiliById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetTafsiliByIdQueryValidator : AbstractValidator<GetTafsiliByIdQuery>
{
    public GetTafsiliByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
