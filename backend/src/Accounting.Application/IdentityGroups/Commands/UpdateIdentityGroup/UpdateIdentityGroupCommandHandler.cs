using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.IdentityGroups.Commands.UpdateIdentityGroup;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_IDENTITYGROUP"/> row via
/// <see cref="IIdentityGroupRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
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
/// <c>TB_IDENTITYGROUP.VAHEDCODE</c> at face value — see <c>UpdateIdentityGroupCommand</c> XML
/// doc for the explicit scope note on what this does and does not cover.
/// </summary>
public sealed class UpdateIdentityGroupCommandHandler : IRequestHandler<UpdateIdentityGroupCommand>
{
    private readonly IIdentityGroupRepository _identityGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateIdentityGroupCommandHandler(
        IIdentityGroupRepository identityGroupRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _identityGroupRepository = identityGroupRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateIdentityGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = await _identityGroupRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("IdentityGroup", request.Id);
        }

        entity.IDENTITYGROUPS_DESC = request.IdentityGroupsDesc;
        entity.IDENTITYGROUPS_CODE = request.IdentityGroupsCode;
        entity.VAHEDCODE = request.VahedCode;
        entity.TAFSILI_ID = request.TafsiliId;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
