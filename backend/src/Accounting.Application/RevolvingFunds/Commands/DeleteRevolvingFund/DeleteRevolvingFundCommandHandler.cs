using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.RevolvingFunds.Commands.DeleteRevolvingFund;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_REVOLVING_FUND"/> row via
/// <see cref="IRevolvingFundRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely. A row with <c>ISDELETED == null</c>
/// is NOT treated as already-deleted (mirrors <c>DeleteWorkShopCommandHandler</c>) — it is
/// genuinely soft-deleted by this call.
///
/// ⚠️ This does NOT cascade to <c>TB_REVOLVINGFUND_LINK_TAFSILI</c> rows — that table is
/// permanently embedded and out of scope for this batch; no repository/command exists to reach
/// it from here.
/// </summary>
public sealed class DeleteRevolvingFundCommandHandler : IRequestHandler<DeleteRevolvingFundCommand>
{
    private readonly IRevolvingFundRepository _revolvingFundRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDeleteDependencyChecker? _deleteDependencyChecker;

    public DeleteRevolvingFundCommandHandler(
        IRevolvingFundRepository revolvingFundRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDeleteDependencyChecker? deleteDependencyChecker = null)
    {
        _revolvingFundRepository = revolvingFundRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _deleteDependencyChecker = deleteDependencyChecker;
    }

    public async Task Handle(DeleteRevolvingFundCommand request, CancellationToken cancellationToken)
    {
        var entity = await _revolvingFundRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("RevolvingFund", request.Id);
        }

        if (entity.ISDELETED == true)
        {
            return;
        }

        // ریسک ۲-الف: حذف نرم نباید رکوردی را که هنوز جای دیگری استفاده می‌شود بی‌صدا یتیم کند.
        if (_deleteDependencyChecker is not null
            && await _deleteDependencyChecker.FindBlockerAsync(DeleteGuardTarget.RevolvingFund, entity.ID, cancellationToken) is { } blocker)
        {
            throw new DeleteBlockedException("RevolvingFund", entity.ID, blocker);
        }

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
