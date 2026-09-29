using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetReceiptAccounting;

public sealed class GetReceiptAccountingQueryHandler : IRequestHandler<GetReceiptAccountingQuery, ReceiptAccountingDto?>
{
    private readonly ITreasuryReceiptReadRepository _receiptReadRepository;

    public GetReceiptAccountingQueryHandler(ITreasuryReceiptReadRepository receiptReadRepository)
    {
        _receiptReadRepository = receiptReadRepository;
    }

    public Task<ReceiptAccountingDto?> Handle(GetReceiptAccountingQuery request, CancellationToken cancellationToken)
        => _receiptReadRepository.GetAccountingAsync(request.Id, request.VahedCode, cancellationToken);
}
