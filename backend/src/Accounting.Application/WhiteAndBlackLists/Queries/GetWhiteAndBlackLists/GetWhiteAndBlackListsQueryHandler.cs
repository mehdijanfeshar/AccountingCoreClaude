using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackLists;

/// <summary>
/// Delegates straight to <see cref="IWhiteAndBlackListReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetWhiteAndBlackListsQueryHandler : IRequestHandler<GetWhiteAndBlackListsQuery, PagedResult<WhiteAndBlackListDto>>
{
    private readonly IWhiteAndBlackListReadRepository _readRepository;

    public GetWhiteAndBlackListsQueryHandler(IWhiteAndBlackListReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<WhiteAndBlackListDto>> Handle(GetWhiteAndBlackListsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
