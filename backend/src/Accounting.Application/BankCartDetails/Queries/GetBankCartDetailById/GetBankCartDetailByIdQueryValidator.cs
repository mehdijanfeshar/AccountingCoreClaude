using FluentValidation;

namespace Accounting.Application.BankCartDetails.Queries.GetBankCartDetailById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetBankCartDetailByIdQueryValidator : AbstractValidator<GetBankCartDetailByIdQuery>
{
    public GetBankCartDetailByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
