using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Tafsilis.Commands.DeleteTafsili;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_TAFSILI"/> row via
/// <see cref="ITafsiliRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely. <c>ISDELETED</c> is nullable
/// <c>bool</c> here, so <see langword="null"/> and <see langword="false"/> both mean "not yet
/// deleted".
/// </summary>
public sealed class DeleteTafsiliCommandHandler : IRequestHandler<DeleteTafsiliCommand>
{
    private readonly ITafsiliRepository _tafsiliRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDeleteDependencyChecker? _deleteDependencyChecker;

    public DeleteTafsiliCommandHandler(
        ITafsiliRepository tafsiliRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDeleteDependencyChecker? deleteDependencyChecker = null)
    {
        _tafsiliRepository = tafsiliRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _deleteDependencyChecker = deleteDependencyChecker;
    }

    public async Task Handle(DeleteTafsiliCommand request, CancellationToken cancellationToken)
    {
        var entity = await _tafsiliRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("Tafsili", request.Id);
        }

        if (entity.ISDELETED == true)
        {
            return;
        }

        // ریسک ۲-الف: حذف نرم نباید رکوردی را که هنوز جای دیگری استفاده می‌شود بی‌صدا یتیم کند.
        if (_deleteDependencyChecker is not null
            && await _deleteDependencyChecker.FindBlockerAsync(DeleteGuardTarget.Tafsili, entity.ID, cancellationToken) is { } blocker)
        {
            throw new DeleteBlockedException("Tafsili", entity.ID, blocker);
        }

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
