using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CreateTreasuryRole;

public sealed class CreateTreasuryRoleCommandHandler : IRequestHandler<CreateTreasuryRoleCommand, Guid>
{
    private readonly ITreasuryRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateTreasuryRoleCommandHandler(
        ITreasuryRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateTreasuryRoleCommand request, CancellationToken cancellationToken)
    {
        var callerRoles = await _roleRepository.GetActiveRolesAsync(request.VahedCode, _currentUser.UserId, cancellationToken);

        if (!callerRoles.Contains(TreasuryRole.FinanceManager))
        {
            // Bootstrap exception (صاحب پروژه، ۲۰۲۶-۰۹-۲۸): the only way out of "no FinanceManager,
            // so nobody may create the first FinanceManager" — see CreateTreasuryRoleCommand XML doc.
            var hasFinanceManager = await _roleRepository.HasActiveFinanceManagerAsync(request.VahedCode, cancellationToken);

            if (hasFinanceManager)
            {
                throw new TreasuryRoleRequiredException(request.VahedCode, TreasuryRole.FinanceManager);
            }
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var existing = await _roleRepository.GetByVahedUserAndRoleAsync(
            request.VahedCode, request.UserId, request.Role, cancellationToken);

        if (existing is null)
        {
            var role = new TB_TR_ROLE
            {
                ID = Guid.NewGuid(),
                VAHEDCODE = request.VahedCode,
                USERID = request.UserId,
                USER_NAME = request.UserName,
                ROLE = request.Role,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            };

            await _roleRepository.AddAsync(role, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return role.ID;
        }

        existing.USER_NAME = request.UserName;
        existing.ISDELETED = false;
        existing.CHANGEUSERID = userId;
        existing.UPDATEDDATE = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return existing.ID;
    }
}
