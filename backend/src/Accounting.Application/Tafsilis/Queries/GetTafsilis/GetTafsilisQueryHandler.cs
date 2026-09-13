using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Tafsilis.Queries.GetTafsilis;

/// <summary>
/// Delegates straight to <see cref="ITafsiliReadRepository.GetPagedAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetTafsilisQueryHandler : IRequestHandler<GetTafsilisQuery, PagedResult<TafsiliDto>>
{
    private readonly ITafsiliReadRepository _readRepository;

    public GetTafsilisQueryHandler(ITafsiliReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<TafsiliDto>> Handle(GetTafsilisQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, request.VahedCode, cancellationToken);
}
