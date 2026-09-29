using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetTransfers;

public sealed class GetTransfersQueryHandler : IRequestHandler<GetTransfersQuery, TransferListResult>
{
    private readonly ITreasuryTransferReadRepository _transferReadRepository;

    public GetTransfersQueryHandler(ITreasuryTransferReadRepository transferReadRepository)
    {
        _transferReadRepository = transferReadRepository;
    }

    public Task<TransferListResult> Handle(GetTransfersQuery request, CancellationToken cancellationToken)
        => _transferReadRepository.GetPagedAsync(
            request.PageNumber, request.PageSize, request.State, request.Search, request.VahedCode, cancellationToken);
}
