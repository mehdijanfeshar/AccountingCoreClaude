using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetTransferAccounting;

public sealed class GetTransferAccountingQueryHandler : IRequestHandler<GetTransferAccountingQuery, TransferAccountingDto?>
{
    private readonly ITreasuryTransferReadRepository _transferReadRepository;

    public GetTransferAccountingQueryHandler(ITreasuryTransferReadRepository transferReadRepository)
    {
        _transferReadRepository = transferReadRepository;
    }

    public Task<TransferAccountingDto?> Handle(GetTransferAccountingQuery request, CancellationToken cancellationToken)
        => _transferReadRepository.GetAccountingAsync(request.Id, request.VahedCode, cancellationToken);
}
