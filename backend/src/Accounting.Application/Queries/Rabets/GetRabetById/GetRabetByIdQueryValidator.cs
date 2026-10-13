using FluentValidation;

namespace Accounting.Application.Queries.Rabets.GetRabetById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetRabetByIdQueryValidator : AbstractValidator<GetRabetByIdQuery>
{
    public GetRabetByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
