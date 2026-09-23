using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Commands.BlacklistWhiteAndBlackList;

/// <summary>
/// Loads the row change-tracked, sets <c>STATE</c> to
/// <see cref="WhiteBlackListState.Blacklisted"/>, clears all four date columns, stamps the audit
/// columns and saves once. Throws <see cref="NotFoundException"/> — mapped to 404 by
/// <c>GlobalExceptionHandler</c> — when the row does not exist or is already soft-deleted;
/// <c>ISDELETED</c> is <c>bool?</c> here, so both <see langword="false"/> and
/// <see langword="null"/> count as "not deleted", consistent with the read side's
/// <c>ISDELETED != true</c> filter.
///
/// Blacklisting an already-blacklisted row is deliberately allowed and is a no-op in effect: the
/// operation is idempotent, and rejecting it would mean a second click on a stale grid returns an
/// error for a state the user already wanted.
/// </summary>
public sealed class BlacklistWhiteAndBlackListCommandHandler : IRequestHandler<BlacklistWhiteAndBlackListCommand>
{
    private readonly IWhiteAndBlackListRepository _whiteAndBlackListRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public BlacklistWhiteAndBlackListCommandHandler(
        IWhiteAndBlackListRepository whiteAndBlackListRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _whiteAndBlackListRepository = whiteAndBlackListRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(BlacklistWhiteAndBlackListCommand request, CancellationToken cancellationToken)
    {
        var entity = await _whiteAndBlackListRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("WhiteAndBlackList", request.Id);
        }

        entity.STATE = WhiteBlackListState.Blacklisted;

        // All four, not just the pair that happened to be in use — see the command's XML doc.
        entity.FROMAUTHORIZEDDATE = null;
        entity.TOAUTHORIZEDDATE = null;
        entity.FROMLIMITATIONDATE = null;
        entity.TOLIMITATIONDATE = null;

        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
