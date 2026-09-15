using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.LevelTafsils.Queries.GetLevelTafsils;

/// <summary>
/// Delegates straight to <see cref="ILevelTafsilReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetLevelTafsilsQueryHandler : IRequestHandler<GetLevelTafsilsQuery, PagedResult<LevelTafsilDto>>
{
    private readonly ILevelTafsilReadRepository _readRepository;

    public GetLevelTafsilsQueryHandler(ILevelTafsilReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<LevelTafsilDto>> Handle(GetLevelTafsilsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
