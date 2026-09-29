using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetPaymentRequests;

public sealed class GetPaymentRequestsQueryHandler : IRequestHandler<GetPaymentRequestsQuery, PaymentRequestListResult>
{
    private readonly IPaymentRequestReadRepository _paymentRequestReadRepository;

    public GetPaymentRequestsQueryHandler(IPaymentRequestReadRepository paymentRequestReadRepository)
    {
        _paymentRequestReadRepository = paymentRequestReadRepository;
    }

    public Task<PaymentRequestListResult> Handle(GetPaymentRequestsQuery request, CancellationToken cancellationToken)
        => _paymentRequestReadRepository.GetPagedAsync(
            request.PageNumber, request.PageSize, request.State, request.Search, request.ForExecution, request.VahedCode, cancellationToken);
}
