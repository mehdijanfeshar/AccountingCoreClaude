using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Receipts.Commands.DeleteReceipt;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_RECEIP"/> row via
/// <see cref="IReceiptRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely. <c>ISDELETED</c> is a non-nullable
/// <c>bool</c> on this table, so there is no NULL edge case to consider (mirrors
/// <c>DeleteChequeTypeCommandHandler</c>).
/// </summary>
public sealed class DeleteReceiptCommandHandler : IRequestHandler<DeleteReceiptCommand>
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteReceiptCommandHandler(
        IReceiptRepository receiptRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _receiptRepository = receiptRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteReceiptCommand request, CancellationToken cancellationToken)
    {
        var entity = await _receiptRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("Receipt", request.Id);
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
