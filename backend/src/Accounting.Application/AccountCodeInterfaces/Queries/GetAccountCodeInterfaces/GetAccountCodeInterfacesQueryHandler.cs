using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Queries.GetAccountCodeInterfaces;

/// <summary>
/// Delegates straight to <see cref="IAccountCodeInterfaceReadRepository.GetPagedAsync"/>.
/// Read-side handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetAccountCodeInterfacesQueryHandler
    : IRequestHandler<GetAccountCodeInterfacesQuery, PagedResult<AccountCodeInterfaceDto>>
{
    private readonly IAccountCodeInterfaceReadRepository _readRepository;

    public GetAccountCodeInterfacesQueryHandler(IAccountCodeInterfaceReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<AccountCodeInterfaceDto>> Handle(GetAccountCodeInterfacesQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
