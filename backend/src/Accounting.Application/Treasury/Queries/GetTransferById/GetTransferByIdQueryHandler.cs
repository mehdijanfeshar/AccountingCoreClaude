using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetTransferById;

public sealed class GetTransferByIdQueryHandler : IRequestHandler<GetTransferByIdQuery, TransferDto?>
{
    private readonly ITreasuryTransferReadRepository _transferReadRepository;

    public GetTransferByIdQueryHandler(ITreasuryTransferReadRepository transferReadRepository)
    {
        _transferReadRepository = transferReadRepository;
    }

    public Task<TransferDto?> Handle(GetTransferByIdQuery request, CancellationToken cancellationToken)
        => _transferReadRepository.GetByIdAsync(request.Id, request.VahedCode, cancellationToken);
}
