using FluentValidation;

namespace Accounting.Application.Queries.PettyCash.GetPettyCashFundById;

public sealed class GetPettyCashFundByIdQueryValidator : AbstractValidator<GetPettyCashFundByIdQuery>
{
    public GetPettyCashFundByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
    }
}
