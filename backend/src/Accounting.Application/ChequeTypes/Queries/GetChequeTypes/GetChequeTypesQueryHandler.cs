using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ChequeTypes.Queries.GetChequeTypes;

/// <summary>
/// Delegates straight to <see cref="IChequeTypeReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetChequeTypesQueryHandler : IRequestHandler<GetChequeTypesQuery, PagedResult<ChequeTypeDto>>
{
    private readonly IChequeTypeReadRepository _readRepository;

    public GetChequeTypesQueryHandler(IChequeTypeReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<ChequeTypeDto>> Handle(GetChequeTypesQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, request.VahedCode, cancellationToken);
}
