using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.WorkShops.Queries.GetWorkShops;

/// <summary>
/// Delegates straight to <see cref="IWorkShopReadRepository.GetPagedAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetWorkShopsQueryHandler : IRequestHandler<GetWorkShopsQuery, PagedResult<WorkShopDto>>
{
    private readonly IWorkShopReadRepository _readRepository;

    public GetWorkShopsQueryHandler(IWorkShopReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<WorkShopDto>> Handle(GetWorkShopsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
