using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.BillLogs.Queries.GetBillLogs;

/// <summary>
/// Delegates straight to <see cref="IBillLogReadRepository.GetPagedAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class GetBillLogsQueryHandler : IRequestHandler<GetBillLogsQuery, PagedResult<BillLogDto>>
{
    private readonly IBillLogReadRepository _readRepository;

    public GetBillLogsQueryHandler(IBillLogReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<BillLogDto>> Handle(GetBillLogsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
