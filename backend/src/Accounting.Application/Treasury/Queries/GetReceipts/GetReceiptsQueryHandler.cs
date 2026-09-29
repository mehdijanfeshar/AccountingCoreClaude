using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetReceipts;

public sealed class GetReceiptsQueryHandler : IRequestHandler<GetReceiptsQuery, ReceiptListResult>
{
    private readonly ITreasuryReceiptReadRepository _receiptReadRepository;

    public GetReceiptsQueryHandler(ITreasuryReceiptReadRepository receiptReadRepository)
    {
        _receiptReadRepository = receiptReadRepository;
    }

    public Task<ReceiptListResult> Handle(GetReceiptsQuery request, CancellationToken cancellationToken)
        => _receiptReadRepository.GetPagedAsync(
            request.PageNumber, request.PageSize, request.State, request.Search, request.VahedCode, cancellationToken);
}
