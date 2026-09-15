using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.IdentitySubGroups.Commands.CreateIdentitySubGroup;

/// <summary>
/// Constructs the <see cref="TB_IDENTITYSUBGRP"/> Domain entity from the command, stages it via
/// <see cref="IIdentitySubGroupRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
///
/// <c>request.VahedCode</c> is equally unforgeable, just enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler can trust the field at face value and
/// simply map it onto <c>TB_IDENTITYSUBGRP.VAHEDCODE</c> — it does not read
/// <see cref="ICurrentUser"/> directly for this field the way it does for <c>ADDUSERID</c>.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>).
/// </summary>
public sealed class CreateIdentitySubGroupCommandHandler : IRequestHandler<CreateIdentitySubGroupCommand, Guid>
{
    private readonly IIdentitySubGroupRepository _identitySubGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateIdentitySubGroupCommandHandler(
        IIdentitySubGroupRepository identitySubGroupRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _identitySubGroupRepository = identitySubGroupRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateIdentitySubGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_IDENTITYSUBGRP
        {
            ID = Guid.NewGuid(),
            IDENTYGROUPS_ID = request.IdentyGroupsId,
            SUBGRPS_DESC = request.SubgrpsDesc,
            SUBGRPS_LEN = request.SubgrpsLen,
            SUMFLAG = request.SumFlag,
            FIXED = request.Fixed,
            SUBGRPS_TYPE = request.SubgrpsType,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            IDENTYSUBGROUPS_CODE = request.IdentySubGroupsCode,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _identitySubGroupRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
