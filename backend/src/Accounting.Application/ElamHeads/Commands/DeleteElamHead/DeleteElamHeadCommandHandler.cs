using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ElamHeads.Commands.DeleteElamHead;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ELAMHEAD"/> row via
/// <see cref="IElamHeadRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely. A row with <c>ISDELETED == null</c>
/// is NOT treated as already-deleted (mirrors <c>DeleteRabetCommandHandler</c>) — it is
/// genuinely soft-deleted by this call.
///
/// ⚠️ HEAD ONLY — does NOT cascade to <c>TB_ELAMDETAIL</c> rows; no repository/command exists
/// to reach them from here (the Head/Detail aggregate boundary for this pair is undecided).
/// </summary>
public sealed class DeleteElamHeadCommandHandler : IRequestHandler<DeleteElamHeadCommand>
{
    private readonly IElamHeadRepository _elamHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDeleteDependencyChecker? _deleteDependencyChecker;

    public DeleteElamHeadCommandHandler(
        IElamHeadRepository elamHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDeleteDependencyChecker? deleteDependencyChecker = null)
    {
        _elamHeadRepository = elamHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _deleteDependencyChecker = deleteDependencyChecker;
    }

    public async Task Handle(DeleteElamHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _elamHeadRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("ElamHead", request.Id);
        }

        if (entity.ISDELETED == true)
        {
            return;
        }

        // ریسک ۲-الف: حذف نرم نباید رکوردی را که هنوز جای دیگری استفاده می‌شود بی‌صدا یتیم کند.
        if (_deleteDependencyChecker is not null
            && await _deleteDependencyChecker.FindBlockerAsync(DeleteGuardTarget.ElamHead, entity.ID, cancellationToken) is { } blocker)
        {
            throw new DeleteBlockedException("ElamHead", entity.ID, blocker);
        }

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
