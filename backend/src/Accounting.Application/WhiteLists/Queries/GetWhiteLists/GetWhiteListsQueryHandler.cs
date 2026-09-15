using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.WhiteLists.Queries.GetWhiteLists;

/// <summary>
/// Delegates straight to <see cref="IWhiteListReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetWhiteListsQueryHandler : IRequestHandler<GetWhiteListsQuery, PagedResult<WhiteListDto>>
{
    private readonly IWhiteListReadRepository _readRepository;

    public GetWhiteListsQueryHandler(IWhiteListReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<WhiteListDto>> Handle(GetWhiteListsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
