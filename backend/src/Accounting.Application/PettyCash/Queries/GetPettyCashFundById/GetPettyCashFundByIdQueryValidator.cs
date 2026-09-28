using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundById;

public sealed class GetPettyCashFundByIdQueryValidator : AbstractValidator<GetPettyCashFundByIdQuery>
{
    public GetPettyCashFundByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
