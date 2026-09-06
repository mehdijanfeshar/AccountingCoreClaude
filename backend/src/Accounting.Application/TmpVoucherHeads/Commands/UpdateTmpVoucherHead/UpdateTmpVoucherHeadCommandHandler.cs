using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.TmpVoucherHeads.Commands.UpdateTmpVoucherHead;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_TMP_VOUCHERHEAD"/> row via
/// <see cref="ITmpVoucherHeadRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted.
///
/// <c>ISDELETED</c> is <c>bool?</c> on this table, so both <see langword="false"/> and
/// <see langword="null"/> are treated as "not deleted" — only an explicit <see langword="true"/>
/// triggers 404, consistent with the <c>ISDELETED != true</c> filter used by the read side.
/// (Contrast <c>UpdatePayReciveHeadCommandHandler</c>, whose column is non-nullable.)
/// <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> — never from the request.
///
/// ⚠️ HEAD ONLY — no <c>TB_TMP_VOUCHERSDETAIL</c> row is ever touched here. See
/// <see cref="UpdateTmpVoucherHeadCommand"/> XML doc.
/// </summary>
public sealed class UpdateTmpVoucherHeadCommandHandler : IRequestHandler<UpdateTmpVoucherHeadCommand>
{
    private readonly ITmpVoucherHeadRepository _tmpVoucherHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateTmpVoucherHeadCommandHandler(
        ITmpVoucherHeadRepository tmpVoucherHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _tmpVoucherHeadRepository = tmpVoucherHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateTmpVoucherHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _tmpVoucherHeadRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("TmpVoucherHead", request.Id);
        }

        entity.VOUCHERSHEAD_ID = request.VoucherHeadId;
        entity.DATE_DOC = request.DateDoc;
        entity.HEAD_DESC = request.HeadDesc;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.SYS_TYPE = request.SysType;
        entity.SOURCEID = request.SourceId;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
