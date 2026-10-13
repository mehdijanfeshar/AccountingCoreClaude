using FluentValidation;

namespace Accounting.Application.Queries.Treasury.GetPaymentRequestById;

public sealed class GetPaymentRequestByIdQueryValidator : AbstractValidator<GetPaymentRequestByIdQuery>
{
    public GetPaymentRequestByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
