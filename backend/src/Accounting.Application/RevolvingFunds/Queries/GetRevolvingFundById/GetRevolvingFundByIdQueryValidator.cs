using FluentValidation;

namespace Accounting.Application.RevolvingFunds.Queries.GetRevolvingFundById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetRevolvingFundByIdQueryValidator : AbstractValidator<GetRevolvingFundByIdQuery>
{
    public GetRevolvingFundByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
