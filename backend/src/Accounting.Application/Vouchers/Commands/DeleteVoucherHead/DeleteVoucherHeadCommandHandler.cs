using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.DeleteVoucherHead;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_VOUCHERSHEAD"/> row via
/// <see cref="IVoucherHeadRepository.GetForUpdateAsync"/> and soft-deletes it, cascading the
/// soft-delete down the full detail tree — both <see cref="Accounting.Domain.Entity.TB_VOUCHERSDETAIL"/>
/// lines AND their <see cref="Accounting.Domain.Entity.TB_VOUCHERDETAIL_LINK_TAFSILI"/> links —
/// via <see cref="IVoucherHeadRepository.SoftDeleteDetailTreeAsync"/>, in the SAME
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call, so the head and its whole subtree never end
/// up in a partially-deleted state. The head and every cascaded row (both levels) are stamped
/// with the same <c>changeUserId</c>/<c>updatedDate</c> pair. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// head row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already
/// soft-deleted (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 —
/// it does NOT re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> on the head, does NOT touch any
/// row in the detail tree (neither level), and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely, since nothing changed and there is
/// nothing to persist.
/// </summary>
public sealed class DeleteVoucherHeadCommandHandler : IRequestHandler<DeleteVoucherHeadCommand>
{
    private readonly IVoucherHeadRepository _voucherHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteVoucherHeadCommandHandler(
        IVoucherHeadRepository voucherHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _voucherHeadRepository = voucherHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteVoucherHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _voucherHeadRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("VoucherHead", request.Id);
        }

        if (entity.ISDELETED == true)
        {
            return;
        }

        var now = DateTime.UtcNow;

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = now;

        await _voucherHeadRepository.SoftDeleteDetailTreeAsync(
            entity.ID,
            _currentUser.UserId,
            now,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
