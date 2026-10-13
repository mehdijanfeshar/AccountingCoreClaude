using Accounting.Application.PayReciveHeads;
using FluentValidation;

namespace Accounting.Application.Queries.PayReciveHeads.GetPayReciveHeadById;

/// <summary>
/// Surface-level (syntactic) validation only — rejects an empty <see cref="Guid"/>.
/// </summary>
public sealed class GetPayReciveHeadByIdQueryValidator : AbstractValidator<GetPayReciveHeadByIdQuery>
{
    public GetPayReciveHeadByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);
    }
}
