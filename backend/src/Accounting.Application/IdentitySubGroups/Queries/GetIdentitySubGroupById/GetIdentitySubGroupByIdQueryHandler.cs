using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroupById;

/// <summary>
/// Delegates straight to <see cref="IIdentitySubGroupReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetIdentitySubGroupByIdQueryHandler : IRequestHandler<GetIdentitySubGroupByIdQuery, IdentitySubGroupDto?>
{
    private readonly IIdentitySubGroupReadRepository _readRepository;

    public GetIdentitySubGroupByIdQueryHandler(IIdentitySubGroupReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<IdentitySubGroupDto?> Handle(GetIdentitySubGroupByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
