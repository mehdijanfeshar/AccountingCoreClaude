using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.CheckBooks.Commands.DeleteCheckBook;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_CHECKBOOK"/> row via
/// <see cref="ICheckBookRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely. <c>ISDELETED</c> is non-nullable
/// <see cref="bool"/> on this table (unlike <c>TB_ACCOUNT</c>), so no NULL-handling ambiguity
/// exists here.
/// </summary>
public sealed class DeleteCheckBookCommandHandler : IRequestHandler<DeleteCheckBookCommand>
{
    private readonly ICheckBookRepository _checkBookRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteCheckBookCommandHandler(
        ICheckBookRepository checkBookRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _checkBookRepository = checkBookRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCheckBookCommand request, CancellationToken cancellationToken)
    {
        var entity = await _checkBookRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("CheckBook", request.Id);
        }

        if (entity.ISDELETED)
        {
            return;
        }

        // عین مرجع: دسته‌چکی که برگی از آن استفاده شده حذف نمی‌شود؛ وگرنه اوراق هم حذف نرم می‌شوند.
        var leaves = await _checkBookRepository.GetLeavesForUpdateAsync(entity.ID, cancellationToken);
        if (leaves.Any(CheckBookLeaves.IsUsed)
            || await _checkBookRepository.CountLeavesInVouchersAsync(entity.ID, cancellationToken) > 0)
        {
            throw new ChequeConflictException("اوراق دسته‌چک انتخابی استفاده شده و قابل حذف نیست.");
        }

        foreach (var leaf in leaves)
        {
            leaf.ISDELETED = true;
            leaf.CHANGEUSERID = _currentUser.UserId;
            leaf.UPDATEDDATE = DateTime.UtcNow;
        }

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
