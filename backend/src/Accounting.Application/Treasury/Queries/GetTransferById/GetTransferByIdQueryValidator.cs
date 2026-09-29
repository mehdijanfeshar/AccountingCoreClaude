using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetTransferById;

public sealed class GetTransferByIdQueryValidator : AbstractValidator<GetTransferByIdQuery>
{
    public GetTransferByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
