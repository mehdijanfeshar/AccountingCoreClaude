using FluentValidation;

namespace Accounting.Application.Queries.IdentityHeads.GetIdentityHeadById;

public sealed class GetIdentityHeadByIdQueryValidator : AbstractValidator<GetIdentityHeadByIdQuery>
{
    public GetIdentityHeadByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
