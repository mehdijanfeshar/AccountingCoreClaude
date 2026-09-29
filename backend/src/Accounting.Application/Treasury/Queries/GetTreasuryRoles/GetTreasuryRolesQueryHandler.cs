using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetTreasuryRoles;

public sealed class GetTreasuryRolesQueryHandler : IRequestHandler<GetTreasuryRolesQuery, IReadOnlyList<TreasuryRoleDto>>
{
    private readonly ITreasuryRoleReadRepository _roleReadRepository;

    public GetTreasuryRolesQueryHandler(ITreasuryRoleReadRepository roleReadRepository)
    {
        _roleReadRepository = roleReadRepository;
    }

    public Task<IReadOnlyList<TreasuryRoleDto>> Handle(GetTreasuryRolesQuery request, CancellationToken cancellationToken)
        => _roleReadRepository.GetByVahedAsync(request.VahedCode, cancellationToken);
}
