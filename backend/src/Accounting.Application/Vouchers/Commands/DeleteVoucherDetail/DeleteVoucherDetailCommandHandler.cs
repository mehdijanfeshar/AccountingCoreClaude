using Accounting.Application.Common.Security;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.DeleteVoucherDetail;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_VOUCHERSDETAIL"/> row via
/// <see cref="IVoucherDetailRepository.GetForUpdateAsync"/> and soft-deletes it, cascading the
/// soft-delete to its own <see cref="Accounting.Domain.Entity.TB_VOUCHERDETAIL_LINK_TAFSILI"/>
/// rows via <see cref="IVoucherDetailRepository.SoftDeleteTafsiliLinksAsync"/>, in the SAME
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call, so the detail row and its tafsili links never
/// end up in a partially-deleted state. Both are stamped with the same
/// <c>changeUserId</c>/<c>updatedDate</c> pair. Throws <see cref="NotFoundException"/> — mapped
/// to 404 by <c>GlobalExceptionHandler</c> — when the detail row does not exist at all.
///
/// Applying the phase-9 invariant "after a soft-delete, nothing beneath it remains active" one
/// level down (from head→detail to detail→tafsili-link) is a direct, non-invented consequence of
/// the team rule that every <c>*_LINK_TAFSIL*</c> table is always embedded and never
/// independently writable — not a new business rule introduced here.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — copied exactly
/// from <c>DeleteVoucherHeadCommandHandler</c>'s pattern. It does NOT re-stamp
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c>, does NOT touch the tafsili links, and deliberately
/// skips <see cref="IUnitOfWork.SaveChangesAsync"/> entirely, since nothing changed.
/// </summary>
public sealed class DeleteVoucherDetailCommandHandler : IRequestHandler<DeleteVoucherDetailCommand>
{
    private readonly IVoucherDetailRepository _voucherDetailRepository;
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteVoucherDetailCommandHandler(
        IVoucherDetailRepository voucherDetailRepository,
        IVoucherHeadRepository voucherHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _voucherDetailRepository = voucherDetailRepository;
        _voucherHeadRepository = voucherHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteVoucherDetailCommand request, CancellationToken cancellationToken)
    {
        var entity = await _voucherDetailRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("VoucherDetail", request.Id);
        }

        // Phase 38: a line inherits its parent voucher's editability — a reviewed/accepted
        // voucher is view-only all the way down. The parent is loaded with the caller's
        // VahedCode, so this cannot be used to probe another unit's voucher either.
        if (entity.VOUCHERSHEAD_ID is not null)
        {
            var head = await _voucherHeadRepository.GetForUpdateAsync(
                entity.VOUCHERSHEAD_ID.Value,
                request.VahedCode,
                cancellationToken);

            if (head is not null)
            {
                VoucherEditability.EnsureEditable(head.ID, head.DOCLIFE);
            }
        }

        if (entity.ISDELETED == true)
        {
            return;
        }

        var now = DateTime.UtcNow;

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = now;

        await _voucherDetailRepository.SoftDeleteTafsiliLinksAsync(
            entity.ID,
            _currentUser.UserId,
            now,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
