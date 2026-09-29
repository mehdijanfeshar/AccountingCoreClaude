using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetReceiptById;

public sealed class GetReceiptByIdQueryValidator : AbstractValidator<GetReceiptByIdQuery>
{
    public GetReceiptByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
