using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AttribForAccountCodes.Commands.DeleteAttribForAccountCode;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ATTRIBFORACCOUNTCODE"/> row via
/// <see cref="IAttribForAccountCodeRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely.
/// </summary>
public sealed class DeleteAttribForAccountCodeCommandHandler : IRequestHandler<DeleteAttribForAccountCodeCommand>
{
    private readonly IAttribForAccountCodeRepository _attribForAccountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDeleteDependencyChecker? _deleteDependencyChecker;

    public DeleteAttribForAccountCodeCommandHandler(
        IAttribForAccountCodeRepository attribForAccountCodeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDeleteDependencyChecker? deleteDependencyChecker = null)
    {
        _attribForAccountCodeRepository = attribForAccountCodeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _deleteDependencyChecker = deleteDependencyChecker;
    }

    public async Task Handle(DeleteAttribForAccountCodeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _attribForAccountCodeRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("AttribForAccountCode", request.Id);
        }

        if (entity.ISDELETED)
        {
            return;
        }

        // ریسک ۲-الف: حذف نرم نباید رکوردی را که هنوز جای دیگری استفاده می‌شود بی‌صدا یتیم کند.
        if (_deleteDependencyChecker is not null
            && await _deleteDependencyChecker.FindBlockerAsync(DeleteGuardTarget.AttribForAccountCode, entity.ID, cancellationToken) is { } blocker)
        {
            throw new DeleteBlockedException("AttribForAccountCode", entity.ID, blocker);
        }

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
