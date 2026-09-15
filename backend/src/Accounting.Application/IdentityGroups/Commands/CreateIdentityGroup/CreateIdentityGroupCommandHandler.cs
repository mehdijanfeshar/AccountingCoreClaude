using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.IdentityGroups.Commands.CreateIdentityGroup;

/// <summary>
/// Constructs the <see cref="TB_IDENTITYGROUP"/> Domain entity from the command, stages it via
/// <see cref="IIdentityGroupRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it
/// cannot be forged by the client.
///
/// <c>request.VahedCode</c> is equally unforgeable, just enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler can trust the field at face value and
/// simply map it onto <c>TB_IDENTITYGROUP.VAHEDCODE</c> — it does not read
/// <see cref="ICurrentUser"/> directly for this field the way it does for <c>ADDUSERID</c>.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>); the Oracle
/// column has no <c>sys_guid()</c> default, so this simply follows the project-wide convention.
/// </summary>
public sealed class CreateIdentityGroupCommandHandler : IRequestHandler<CreateIdentityGroupCommand, Guid>
{
    private readonly IIdentityGroupRepository _identityGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateIdentityGroupCommandHandler(
        IIdentityGroupRepository identityGroupRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _identityGroupRepository = identityGroupRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateIdentityGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_IDENTITYGROUP
        {
            ID = Guid.NewGuid(),
            IDENTITYGROUPS_DESC = request.IdentityGroupsDesc,
            IDENTITYGROUPS_CODE = request.IdentityGroupsCode,
            VAHEDCODE = request.VahedCode,
            TAFSILI_ID = request.TafsiliId,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _identityGroupRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
