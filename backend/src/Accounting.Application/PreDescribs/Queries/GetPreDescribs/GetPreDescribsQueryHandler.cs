using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PreDescribs.Queries.GetPreDescribs;

/// <summary>
/// Delegates straight to <see cref="IPreDescribReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetPreDescribsQueryHandler : IRequestHandler<GetPreDescribsQuery, PagedResult<PreDescribDto>>
{
    private readonly IPreDescribReadRepository _readRepository;

    public GetPreDescribsQueryHandler(IPreDescribReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<PreDescribDto>> Handle(GetPreDescribsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
