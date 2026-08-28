using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroups;

/// <summary>
/// Delegates straight to <see cref="IIdentitySubGroupReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetIdentitySubGroupsQueryHandler : IRequestHandler<GetIdentitySubGroupsQuery, PagedResult<IdentitySubGroupDto>>
{
    private readonly IIdentitySubGroupReadRepository _readRepository;

    public GetIdentitySubGroupsQueryHandler(IIdentitySubGroupReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<IdentitySubGroupDto>> Handle(GetIdentitySubGroupsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
