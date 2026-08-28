using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.IdentityGroups.Queries.GetIdentityGroupById;

/// <summary>
/// Delegates straight to <see cref="IIdentityGroupReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetIdentityGroupByIdQueryHandler : IRequestHandler<GetIdentityGroupByIdQuery, IdentityGroupDto?>
{
    private readonly IIdentityGroupReadRepository _readRepository;

    public GetIdentityGroupByIdQueryHandler(IIdentityGroupReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<IdentityGroupDto?> Handle(GetIdentityGroupByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
