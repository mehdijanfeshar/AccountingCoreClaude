using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ChequeTypes.Commands.DeleteChequeType;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_CHECK_TYPE"/> row via
/// <see cref="IChequeTypeRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely.
/// </summary>
public sealed class DeleteChequeTypeCommandHandler : IRequestHandler<DeleteChequeTypeCommand>
{
    private readonly IChequeTypeRepository _chequeTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDeleteDependencyChecker? _deleteDependencyChecker;

    public DeleteChequeTypeCommandHandler(
        IChequeTypeRepository chequeTypeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDeleteDependencyChecker? deleteDependencyChecker = null)
    {
        _chequeTypeRepository = chequeTypeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _deleteDependencyChecker = deleteDependencyChecker;
    }

    public async Task Handle(DeleteChequeTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _chequeTypeRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("ChequeType", request.Id);
        }

        if (entity.ISDELETED)
        {
            return;
        }

        // ریسک ۲-الف: حذف نرم نباید رکوردی را که هنوز جای دیگری استفاده می‌شود بی‌صدا یتیم کند.
        if (_deleteDependencyChecker is not null
            && await _deleteDependencyChecker.FindBlockerAsync(DeleteGuardTarget.ChequeType, entity.ID, cancellationToken) is { } blocker)
        {
            throw new DeleteBlockedException("ChequeType", entity.ID, blocker);
        }

        entity.ISDELETED = true;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
