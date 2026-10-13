using FluentValidation;

namespace Accounting.Application.Queries.AccountCodeInterfaces.GetAccountCodeInterfaceById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetAccountCodeInterfaceByIdQueryValidator : AbstractValidator<GetAccountCodeInterfaceByIdQuery>
{
    public GetAccountCodeInterfaceByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
