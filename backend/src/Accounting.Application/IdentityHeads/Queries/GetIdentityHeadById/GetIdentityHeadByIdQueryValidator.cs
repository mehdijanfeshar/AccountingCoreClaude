using FluentValidation;

namespace Accounting.Application.IdentityHeads.Queries.GetIdentityHeadById;

public sealed class GetIdentityHeadByIdQueryValidator : AbstractValidator<GetIdentityHeadByIdQuery>
{
    public GetIdentityHeadByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
