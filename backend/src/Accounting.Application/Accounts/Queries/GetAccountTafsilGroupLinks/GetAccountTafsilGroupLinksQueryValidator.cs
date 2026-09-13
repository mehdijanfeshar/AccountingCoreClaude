using FluentValidation;

namespace Accounting.Application.Accounts.Queries.GetAccountTafsilGroupLinks;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetAccountTafsilGroupLinksQueryValidator : AbstractValidator<GetAccountTafsilGroupLinksQuery>
{
    public GetAccountTafsilGroupLinksQueryValidator()
    {
        RuleFor(x => x.AccountCodeId)
            .NotEqual(Guid.Empty);
    }
}
