using FluentValidation;

namespace Accounting.Application.Queries.ChequeTypes.GetChequeTypeById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetChequeTypeByIdQueryValidator : AbstractValidator<GetChequeTypeByIdQuery>
{
    public GetChequeTypeByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
