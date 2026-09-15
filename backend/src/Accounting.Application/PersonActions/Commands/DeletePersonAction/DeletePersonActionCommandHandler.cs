using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PersonActions.Commands.DeletePersonAction;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_PERSON_ACTION"/> row via
/// <see cref="IPersonActionRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> when the row does not exist at all.
///
/// Idempotency: a row that is already soft-deleted is treated as a no-op success rather than a
/// 404 — it does NOT re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely. Note that a soft-deleted row still
/// occupies its slot in <c>UK_PERSON_ACTION</c> (soft delete does not release the unique
/// combination) — a pre-existing constraint characteristic, not something introduced here.
/// </summary>
public sealed class DeletePersonActionCommandHandler : IRequestHandler<DeletePersonActionCommand>
{
    private readonly IPersonActionRepository _personActionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeletePersonActionCommandHandler(
        IPersonActionRepository personActionRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _personActionRepository = personActionRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeletePersonActionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _personActionRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("PersonAction", request.Id);
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
