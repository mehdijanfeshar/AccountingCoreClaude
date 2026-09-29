using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetPaymentRequestAccounting;

public sealed class GetPaymentRequestAccountingQueryHandler
    : IRequestHandler<GetPaymentRequestAccountingQuery, PaymentRequestAccountingDto?>
{
    private readonly IPaymentRequestReadRepository _paymentRequestReadRepository;

    public GetPaymentRequestAccountingQueryHandler(IPaymentRequestReadRepository paymentRequestReadRepository)
    {
        _paymentRequestReadRepository = paymentRequestReadRepository;
    }

    public Task<PaymentRequestAccountingDto?> Handle(GetPaymentRequestAccountingQuery request, CancellationToken cancellationToken)
        => _paymentRequestReadRepository.GetAccountingAsync(request.Id, request.VahedCode, cancellationToken);
}
