using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ChequesIncorrents.Commands.DeleteChequesIncorrent;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_CHEQUES_INCORRENT"/> row via
/// <see cref="IChequesIncorrentRepository.GetForUpdateAsync"/> and soft-deletes it. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist at all.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a row that is already soft-deleted
/// (<c>ISDELETED == true</c>) is treated as a no-op success rather than a 404 — it does NOT
/// re-stamp <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> and deliberately skips the
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call entirely. <c>ISDELETED</c> is non-nullable
/// <see cref="bool"/> on this table, so no NULL-handling ambiguity exists here.
/// </summary>
public sealed class DeleteChequesIncorrentCommandHandler : IRequestHandler<DeleteChequesIncorrentCommand>
{
    private readonly IChequesIncorrentRepository _chequesIncorrentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteChequesIncorrentCommandHandler(
        IChequesIncorrentRepository chequesIncorrentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _chequesIncorrentRepository = chequesIncorrentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteChequesIncorrentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _chequesIncorrentRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException("ChequesIncorrent", request.Id);
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
