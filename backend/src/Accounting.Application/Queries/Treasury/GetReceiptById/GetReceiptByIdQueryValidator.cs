using FluentValidation;

namespace Accounting.Application.Queries.Treasury.GetReceiptById;

public sealed class GetReceiptByIdQueryValidator : AbstractValidator<GetReceiptByIdQuery>
{
    public GetReceiptByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
