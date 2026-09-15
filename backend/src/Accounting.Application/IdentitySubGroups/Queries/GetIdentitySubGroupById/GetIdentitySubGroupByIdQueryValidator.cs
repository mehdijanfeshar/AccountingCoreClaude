using FluentValidation;

namespace Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroupById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetIdentitySubGroupByIdQueryValidator : AbstractValidator<GetIdentitySubGroupByIdQuery>
{
    public GetIdentitySubGroupByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
