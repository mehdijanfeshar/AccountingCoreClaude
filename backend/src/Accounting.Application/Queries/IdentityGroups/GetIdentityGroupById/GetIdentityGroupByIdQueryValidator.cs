using FluentValidation;

namespace Accounting.Application.Queries.IdentityGroups.GetIdentityGroupById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetIdentityGroupByIdQueryValidator : AbstractValidator<GetIdentityGroupByIdQuery>
{
    public GetIdentityGroupByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
