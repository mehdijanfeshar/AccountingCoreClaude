using FluentValidation;

namespace Accounting.Application.Queries.BankAccounts.GetBankAccountById;

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
