using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_VOUCHERSDETAIL"/> row via
/// <see cref="IVoucherDetailRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary
/// by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED == true</c>); a soft-deleted row
/// is treated as logically absent for update purposes, consistent with the <c>ISDELETED != true</c>
/// filter used by the read side and with <c>UpdateVoucherHeadCommandHandler</c>'s identical
/// boundary rule. <c>VOUCHERSHEAD_ID</c> is never touched here (see
/// <see cref="UpdateVoucherDetailCommand"/> XML doc for the "no reparenting" rationale).
/// <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> (the authenticated caller) —
/// never from the request — so it cannot be forged by the client.
/// </summary>
public sealed class UpdateVoucherDetailCommandHandler : IRequestHandler<UpdateVoucherDetailCommand>
{
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateVoucherDetailCommandHandler(
        IVoucherDetailRepository voucherDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _voucherDetailRepository = voucherDetailRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateVoucherDetailCommand request, CancellationToken cancellationToken)
    {
        var entity = await _voucherDetailRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("VoucherDetail", request.Id);
        }

        entity.ACCOUNT_ID = request.AccountId;
        entity.RECEIP_ID = request.ReceiptId;
        entity.CHECK_ID = request.CheckId;
        entity.LOWLEVELCODE_ID = request.LowLevelCodeId;
        entity.ETEBAR_ID = request.EtebarId;
        entity.DESCRIPTION = request.Description;
        entity.RADIF = request.Radif;
        entity.DEBTOR = request.Debtor;
        entity.CREDITOR = request.Creditor;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
