using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.DeleteTreasuryRole;

public sealed class DeleteTreasuryRoleCommandHandler : IRequestHandler<DeleteTreasuryRoleCommand>
{
    private readonly ITreasuryRoleRepository _roleRepository;
    private readonly ITreasuryRoleAuthorizer _roleAuthorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteTreasuryRoleCommandHandler(
        ITreasuryRoleRepository roleRepository,
        ITreasuryRoleAuthorizer roleAuthorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _roleRepository = roleRepository;
        _roleAuthorizer = roleAuthorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteTreasuryRoleCommand request, CancellationToken cancellationToken)
    {
        await _roleAuthorizer.EnsureHasRoleAsync(request.VahedCode, new[] { TreasuryRole.FinanceManager }, cancellationToken);

        var role = await _roleRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (role is null || role.ISDELETED)
        {
            throw new NotFoundException("TreasuryRole", request.Id);
        }

        role.ISDELETED = true;
        role.CHANGEUSERID = _currentUser.UserId;
        role.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
