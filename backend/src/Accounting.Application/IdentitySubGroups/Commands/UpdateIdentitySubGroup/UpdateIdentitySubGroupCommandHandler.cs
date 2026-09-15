using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.IdentitySubGroups.Commands.UpdateIdentitySubGroup;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_IDENTITYSUBGRP"/> row via
/// <see cref="IIdentitySubGroupRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED</c> is non-nullable <c>bool</c>
/// on this table). <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> — never from
/// the request.
///
/// <c>request.VahedCode</c> is likewise unforgeable, enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler simply maps it onto
/// <c>TB_IDENTITYSUBGRP.VAHEDCODE</c> at face value — see <c>UpdateIdentitySubGroupCommand</c>
/// XML doc for the explicit scope note on what this does and does not cover.
/// </summary>
public sealed class UpdateIdentitySubGroupCommandHandler : IRequestHandler<UpdateIdentitySubGroupCommand>
{
    private readonly IIdentitySubGroupRepository _identitySubGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateIdentitySubGroupCommandHandler(
        IIdentitySubGroupRepository identitySubGroupRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _identitySubGroupRepository = identitySubGroupRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateIdentitySubGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = await _identitySubGroupRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("IdentitySubGroup", request.Id);
        }

        entity.IDENTYGROUPS_ID = request.IdentyGroupsId;
        entity.SUBGRPS_DESC = request.SubgrpsDesc;
        entity.SUBGRPS_LEN = request.SubgrpsLen;
        entity.SUMFLAG = request.SumFlag;
        entity.FIXED = request.Fixed;
        entity.SUBGRPS_TYPE = request.SubgrpsType;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.IDENTYSUBGROUPS_CODE = request.IdentySubGroupsCode;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
