using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.BankCartDetails.Commands.DeleteBankCartDetail;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_BANKCARTDETAIL"/> row via
/// <see cref="IBankCartDetailRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely. A row with <c>ISDELETED == null</c>
/// is NOT treated as already-deleted (mirrors <c>DeleteWorkShopCommandHandler</c>) — it is
/// genuinely soft-deleted by this call.
/// </summary>
public sealed class DeleteBankCartDetailCommandHandler : IRequestHandler<DeleteBankCartDetailCommand>
{
    private readonly IBankCartDetailRepository _bankCartDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteBankCartDetailCommandHandler(
        IBankCartDetailRepository bankCartDetailRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _bankCartDetailRepository = bankCartDetailRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteBankCartDetailCommand request, CancellationToken cancellationToken)
    {
        var entity = await _bankCartDetailRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("BankCartDetail", request.Id);
        }

        if (entity.ISDELETED == true)
        {
            return;
        }

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
