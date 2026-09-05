using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ElamHeads.Queries.GetElamHeads;

/// <summary>
/// Delegates straight to <see cref="IElamHeadReadRepository.GetPagedAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetElamHeadsQueryHandler : IRequestHandler<GetElamHeadsQuery, PagedResult<ElamHeadDto>>
{
    private readonly IElamHeadReadRepository _readRepository;

    public GetElamHeadsQueryHandler(IElamHeadReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<ElamHeadDto>> Handle(GetElamHeadsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
