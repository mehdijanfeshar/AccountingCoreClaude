using FluentValidation;

namespace Accounting.Application.Accounts.Queries.GetAccountCodeById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetAccountCodeByIdQueryValidator : AbstractValidator<GetAccountCodeByIdQuery>
{
    public GetAccountCodeByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
