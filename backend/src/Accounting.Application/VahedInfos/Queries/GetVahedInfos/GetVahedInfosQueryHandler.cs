using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.VahedInfos.Queries.GetVahedInfos;

/// <summary>
/// Delegates straight to <see cref="IVahedInfoReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetVahedInfosQueryHandler : IRequestHandler<GetVahedInfosQuery, PagedResult<VahedInfoDto>>
{
    private readonly IVahedInfoReadRepository _readRepository;

    public GetVahedInfosQueryHandler(IVahedInfoReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<VahedInfoDto>> Handle(GetVahedInfosQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
