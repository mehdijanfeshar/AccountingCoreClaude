using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Receipts.Queries.GetReceiptById;

/// <summary>
/// Delegates straight to <see cref="IReceiptReadRepository.GetByIdAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetReceiptByIdQueryHandler : IRequestHandler<GetReceiptByIdQuery, ReceiptDto?>
{
    private readonly IReceiptReadRepository _readRepository;

    public GetReceiptByIdQueryHandler(IReceiptReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<ReceiptDto?> Handle(GetReceiptByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
