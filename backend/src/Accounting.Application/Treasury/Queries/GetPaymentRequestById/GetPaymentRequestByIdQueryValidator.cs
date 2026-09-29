using FluentValidation;

namespace Accounting.Application.Treasury.Queries.GetPaymentRequestById;

public sealed class GetPaymentRequestByIdQueryValidator : AbstractValidator<GetPaymentRequestByIdQuery>
{
    public GetPaymentRequestByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
