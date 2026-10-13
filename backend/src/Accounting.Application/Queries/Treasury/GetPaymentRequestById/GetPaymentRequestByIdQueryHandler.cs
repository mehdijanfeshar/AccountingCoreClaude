using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Queries.Treasury.GetPaymentRequestById;

public sealed class GetPaymentRequestByIdQueryHandler : IRequestHandler<GetPaymentRequestByIdQuery, PaymentRequestDto?>
{
    private readonly IPaymentRequestReadRepository _paymentRequestReadRepository;

    public GetPaymentRequestByIdQueryHandler(IPaymentRequestReadRepository paymentRequestReadRepository)
    {
        _paymentRequestReadRepository = paymentRequestReadRepository;
    }

    public Task<PaymentRequestDto?> Handle(GetPaymentRequestByIdQuery request, CancellationToken cancellationToken)
        => _paymentRequestReadRepository.GetByIdAsync(request.Id, request.VahedCode, cancellationToken);
}
