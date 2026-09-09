using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.IdentityGroups.Queries.GetIdentityGroups;

/// <summary>
/// Delegates straight to <see cref="IIdentityGroupReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
///
/// <c>request.VahedCode</c> is passed through as-is, at face value: by the time this handler
/// runs, <c>VahedScopeBehavior</c> has already overwritten it with the authenticated caller's own
/// unit code, so this handler never reads <see cref="ICurrentUser"/> directly.
/// </summary>
public sealed class GetIdentityGroupsQueryHandler : IRequestHandler<GetIdentityGroupsQuery, PagedResult<IdentityGroupDto>>
{
    private readonly IIdentityGroupReadRepository _readRepository;

    public GetIdentityGroupsQueryHandler(IIdentityGroupReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<IdentityGroupDto>> Handle(GetIdentityGroupsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, request.VahedCode, cancellationToken);
}
