using FluentValidation;

namespace Accounting.Application.TafsilGroups.Queries.GetTafsilGroupById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetTafsilGroupByIdQueryValidator : AbstractValidator<GetTafsilGroupByIdQuery>
{
    public GetTafsilGroupByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
