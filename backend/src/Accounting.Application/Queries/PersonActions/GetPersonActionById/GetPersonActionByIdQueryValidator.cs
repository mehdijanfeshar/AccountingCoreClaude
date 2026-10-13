using FluentValidation;

namespace Accounting.Application.Queries.PersonActions.GetPersonActionById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetPersonActionByIdQueryValidator : AbstractValidator<GetPersonActionByIdQuery>
{
    public GetPersonActionByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
