using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Queries.GetAccountCodeInterfaceById;

/// <summary>
/// Delegates straight to <see cref="IAccountCodeInterfaceReadRepository.GetByIdAsync"/>.
/// Read-side handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetAccountCodeInterfaceByIdQueryHandler
    : IRequestHandler<GetAccountCodeInterfaceByIdQuery, AccountCodeInterfaceDto?>
{
    private readonly IAccountCodeInterfaceReadRepository _readRepository;

    public GetAccountCodeInterfaceByIdQueryHandler(IAccountCodeInterfaceReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<AccountCodeInterfaceDto?> Handle(GetAccountCodeInterfaceByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
