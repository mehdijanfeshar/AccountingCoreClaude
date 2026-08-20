using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Accounts.Commands.DeleteAccountCode;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ACCOUNTCODE"/> row via
/// <see cref="IAccountCodeRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already
/// soft-deleted (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 —
/// it does NOT re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely, since nothing changed and there
/// is nothing to persist.
/// </summary>
public sealed class DeleteAccountCodeCommandHandler : IRequestHandler<DeleteAccountCodeCommand>
{
    private readonly IAccountCodeRepository _accountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteAccountCodeCommandHandler(
        IAccountCodeRepository accountCodeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountCodeRepository = accountCodeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteAccountCodeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _accountCodeRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("AccountCode", request.Id);
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
