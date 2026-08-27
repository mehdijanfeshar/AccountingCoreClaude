using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.BillLogs.Commands.DeleteBillLog;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_BILL_LOG"/> row via
/// <see cref="IBillLogRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> when the row does not exist at all.
///
/// Idempotency: a row that is already soft-deleted is treated as a no-op success rather than a
/// 404 — it does NOT re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely.
/// </summary>
public sealed class DeleteBillLogCommandHandler : IRequestHandler<DeleteBillLogCommand>
{
    private readonly IBillLogRepository _billLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteBillLogCommandHandler(
        IBillLogRepository billLogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _billLogRepository = billLogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteBillLogCommand request, CancellationToken cancellationToken)
    {
        var entity = await _billLogRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("BillLog", request.Id);
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
