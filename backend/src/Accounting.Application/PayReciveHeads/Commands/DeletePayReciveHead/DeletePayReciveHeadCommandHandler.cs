using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PayReciveHeads.Commands.DeletePayReciveHead;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_PAYRECIVHEAD"/> row via
/// <see cref="IPayReciveHeadRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely. <c>ISDELETED</c> is a non-nullable
/// <c>bool</c> on this table, so unlike <c>DeleteTmpVoucherHeadCommandHandler</c> there is no
/// <c>null</c> case to reason about at all.
///
/// ⚠️ HEAD ONLY — does NOT cascade to <c>TB_PAYRECIVDETAIL</c> rows; no repository or command
/// exists to reach them from here (the Head/Detail aggregate boundary for this pair is
/// undecided). Soft-deleting a header therefore leaves its detail rows — and their
/// <c>TB_PAYRECIVDETAIL_LINK_TAFSILI</c> links — active, exactly the situation the voucher had
/// before phase 9.
/// </summary>
public sealed class DeletePayReciveHeadCommandHandler : IRequestHandler<DeletePayReciveHeadCommand>
{
    private readonly IPayReciveHeadRepository _payReciveHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeletePayReciveHeadCommandHandler(
        IPayReciveHeadRepository payReciveHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _payReciveHeadRepository = payReciveHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePayReciveHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _payReciveHeadRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("PayReciveHead", request.Id);
        }

        if (entity.ISDELETED)
        {
            return;
        }

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
