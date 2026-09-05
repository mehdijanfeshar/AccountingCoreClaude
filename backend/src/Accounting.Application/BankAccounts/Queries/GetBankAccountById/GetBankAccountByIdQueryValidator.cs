using FluentValidation;

namespace Accounting.Application.BankAccounts.Queries.GetBankAccountById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetBankAccountByIdQueryValidator : AbstractValidator<GetBankAccountByIdQuery>
{
    public GetBankAccountByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
