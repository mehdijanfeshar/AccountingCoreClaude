using FluentValidation;

namespace Accounting.Application.Queries.Treasury.GetTransferById;

public sealed class GetTransferByIdQueryValidator : AbstractValidator<GetTransferByIdQuery>
{
    public GetTransferByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
