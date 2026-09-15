using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.LevelTafsils.Commands.DeleteLevelTafsil;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_LEVEL_TAFSIL"/> row via
/// <see cref="ILevelTafsilRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely.
///
/// ⚠️ Does NOT check for dependent rows — see <see cref="DeleteLevelTafsilCommand"/> XML doc.
/// </summary>
public sealed class DeleteLevelTafsilCommandHandler : IRequestHandler<DeleteLevelTafsilCommand>
{
    private readonly ILevelTafsilRepository _levelTafsilRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteLevelTafsilCommandHandler(
        ILevelTafsilRepository levelTafsilRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _levelTafsilRepository = levelTafsilRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteLevelTafsilCommand request, CancellationToken cancellationToken)
    {
        var entity = await _levelTafsilRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("LevelTafsil", request.Id);
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
