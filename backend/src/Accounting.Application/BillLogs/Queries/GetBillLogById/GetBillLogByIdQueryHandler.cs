using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.BillLogs.Queries.GetBillLogById;

/// <summary>
/// Delegates straight to <see cref="IBillLogReadRepository.GetByIdAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class GetBillLogByIdQueryHandler : IRequestHandler<GetBillLogByIdQuery, BillLogDto?>
{
    private readonly IBillLogReadRepository _readRepository;

    public GetBillLogByIdQueryHandler(IBillLogReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<BillLogDto?> Handle(GetBillLogByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
