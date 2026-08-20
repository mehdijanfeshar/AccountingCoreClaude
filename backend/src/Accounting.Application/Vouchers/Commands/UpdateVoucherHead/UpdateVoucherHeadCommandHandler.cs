using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.UpdateVoucherHead;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_VOUCHERSHEAD"/> row via
/// <see cref="IVoucherHeadRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary
/// by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED == true</c>); a soft-deleted
/// row is treated as logically absent for update purposes, consistent with the
/// <c>ISDELETED != true</c> filter used by the read side. <c>GLOBALNUMBER</c> and
/// <c>ATTACHFILE</c> are never touched here (see <see cref="UpdateVoucherHeadCommand"/> doc).
/// <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> (the authenticated caller)
/// — never from the request — so it cannot be forged by the client, mirroring the audit
/// pattern already used by <c>CreateVoucherHeadCommandHandler</c>.
/// </summary>
public sealed class UpdateVoucherHeadCommandHandler : IRequestHandler<UpdateVoucherHeadCommand>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateVoucherHeadCommandHandler(
        IVoucherHeadRepository voucherHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateVoucherHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _voucherHeadRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("VoucherHead", request.Id);
        }

        entity.DOC_NUM = request.DocNum;
        entity.DATE_DOC = request.DateDoc;
        entity.DOCLIFE = request.DocLife;
        entity.HEAD_DESC = request.HeadDesc;
        entity.APENDIX = request.Apendix;
        entity.SYSTEM_TYPE = request.SystemTypeId;
        entity.FLAG_STATE = request.FlagState;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.ISAUTOMATIC = request.IsAutomatic;
        entity.SNDVAHEDCODE = request.SndVahedCode;
        entity.PARENTHEAD_ID = request.ParentHeadId;
        entity.ATTACHFILE_NAME = request.AttachFileName;
        entity.ATF_NUM = request.AtfNum;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
