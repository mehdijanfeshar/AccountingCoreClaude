using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Commands.DeleteAccountCodeInterface;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ACCOUNTCODE_INTERFACE"/> row via
/// <see cref="IAccountCodeInterfaceRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely.
/// </summary>
public sealed class DeleteAccountCodeInterfaceCommandHandler : IRequestHandler<DeleteAccountCodeInterfaceCommand>
{
    private readonly IAccountCodeInterfaceRepository _accountCodeInterfaceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteAccountCodeInterfaceCommandHandler(
        IAccountCodeInterfaceRepository accountCodeInterfaceRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountCodeInterfaceRepository = accountCodeInterfaceRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteAccountCodeInterfaceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _accountCodeInterfaceRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("AccountCodeInterface", request.Id);
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
