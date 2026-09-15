using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AccountExceptions.Commands.DeleteAccountException;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ACCOUNTEXCEPTION"/> row via
/// <see cref="IAccountExceptionRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> when the row does not exist at all.
///
/// Idempotency: a row that is already soft-deleted is treated as a no-op success rather than a
/// 404 — it does NOT re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely.
/// </summary>
public sealed class DeleteAccountExceptionCommandHandler : IRequestHandler<DeleteAccountExceptionCommand>
{
    private readonly IAccountExceptionRepository _accountExceptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteAccountExceptionCommandHandler(
        IAccountExceptionRepository accountExceptionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountExceptionRepository = accountExceptionRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteAccountExceptionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _accountExceptionRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("AccountException", request.Id);
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
