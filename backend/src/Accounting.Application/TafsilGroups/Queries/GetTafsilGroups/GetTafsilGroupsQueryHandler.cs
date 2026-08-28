using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.TafsilGroups.Queries.GetTafsilGroups;

/// <summary>
/// Delegates straight to <see cref="ITafsilGroupReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetTafsilGroupsQueryHandler : IRequestHandler<GetTafsilGroupsQuery, PagedResult<TafsilGroupDto>>
{
    private readonly ITafsilGroupReadRepository _readRepository;

    public GetTafsilGroupsQueryHandler(ITafsilGroupReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<TafsilGroupDto>> Handle(GetTafsilGroupsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
