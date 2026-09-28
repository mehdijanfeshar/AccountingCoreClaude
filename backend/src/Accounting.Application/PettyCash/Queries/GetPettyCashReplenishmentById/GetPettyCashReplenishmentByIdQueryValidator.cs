using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashReplenishmentById;

public sealed class GetPettyCashReplenishmentByIdQueryValidator : AbstractValidator<GetPettyCashReplenishmentByIdQuery>
{
    public GetPettyCashReplenishmentByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
