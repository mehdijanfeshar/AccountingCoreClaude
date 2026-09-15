using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Rabets.Queries.GetRabets;

/// <summary>
/// Delegates straight to <see cref="IRabetReadRepository.GetPagedAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetRabetsQueryHandler : IRequestHandler<GetRabetsQuery, PagedResult<RabetDto>>
{
    private readonly IRabetReadRepository _readRepository;

    public GetRabetsQueryHandler(IRabetReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<RabetDto>> Handle(GetRabetsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
