using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Accounts.Commands.UpdateAccountCode;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ACCOUNTCODE"/> row via
/// <see cref="IAccountCodeRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary
/// by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED == true</c>); a soft-deleted
/// row is treated as logically absent for update purposes, consistent with the
/// <c>ISDELETED != true</c> filter used by the read side. <c>CHANGEUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request — so it
/// cannot be forged by the client, mirroring the audit pattern already used by
/// <c>CreateAccountCodeCommandHandler</c>.
/// </summary>
public sealed class UpdateAccountCodeCommandHandler : IRequestHandler<UpdateAccountCodeCommand>
{
    private readonly IAccountCodeRepository _accountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateAccountCodeCommandHandler(
        IAccountCodeRepository accountCodeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountCodeRepository = accountCodeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateAccountCodeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _accountCodeRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("AccountCode", request.Id);
        }

        entity.TYPECODE = request.TypeCode;
        entity.PARENTID = request.ParentId;
        entity.ACCCODE = request.AccCode;
        entity.ACCCODENAME = request.AccCodeName;
        entity.TYPEACTIVITY = request.TypeActivity;
        entity.SOURCEANDCONSUME_ID = request.SourceAndConsumeId;
        entity.IDENTYGROUPS_ID = request.IdentyGroupsId;
        entity.TYPEACCCODE = request.TypeAccCode;
        entity.MOINFORCLOSE = request.MoInforClose;
        entity.TYPEACTION = request.TypeAction;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
