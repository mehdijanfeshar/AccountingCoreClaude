using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Queries.Treasury.GetReceiptById;

public sealed class GetReceiptByIdQueryHandler : IRequestHandler<GetReceiptByIdQuery, ReceiptDto?>
{
    private readonly ITreasuryReceiptReadRepository _receiptReadRepository;

    public GetReceiptByIdQueryHandler(ITreasuryReceiptReadRepository receiptReadRepository)
    {
        _receiptReadRepository = receiptReadRepository;
    }

    public Task<ReceiptDto?> Handle(GetReceiptByIdQuery request, CancellationToken cancellationToken)
        => _receiptReadRepository.GetByIdAsync(request.Id, request.VahedCode, cancellationToken);
}
