using Accounting.Application.Accounts.Commands.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Accounts.Commands.UnlinkAccountCodeFromTafsilGroup;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ACCOUNT_LINK_TAFSILGROUP"/> row via
/// <see cref="IAccountCodeRepository.GetTafsilGroupLinkForUpdateAsync"/> (scoped to the given
/// معین) and soft-deletes it. Throws <see cref="NotFoundException"/> — mapped to 404 — when no
/// such link exists under that معین at all.
///
/// Removing the last گروه تفصیلی of a level also removes that level's requirement:
/// <see cref="AccountLevelLinkSynchronizer"/> runs before the save and retires the
/// <c>TB_ACCOUNT_LINK_LEVEL</c> row, so the voucher form stops demanding a تفصیلی it can no
/// longer offer any values for. A level still covered by another link is left alone.
///
/// Idempotency: HTTP DELETE is defined as idempotent, so a link that is already soft-deleted is
/// treated as a no-op success rather than a 404 — no re-stamp, no level sync, and
/// <see cref="IUnitOfWork.SaveChangesAsync"/> is skipped entirely. The sync belongs to paths that
/// actually change something; running it here would turn a repeated delete into a write.
/// </summary>
public sealed class UnlinkAccountCodeFromTafsilGroupCommandHandler : IRequestHandler<UnlinkAccountCodeFromTafsilGroupCommand>
{
    private readonly IAccountCodeRepository _accountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly AccountLevelLinkSynchronizer _levelLinkSynchronizer;

    public UnlinkAccountCodeFromTafsilGroupCommandHandler(
        IAccountCodeRepository accountCodeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        AccountLevelLinkSynchronizer levelLinkSynchronizer)
    {
        _accountCodeRepository = accountCodeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _levelLinkSynchronizer = levelLinkSynchronizer;
    }

    public async Task Handle(UnlinkAccountCodeFromTafsilGroupCommand request, CancellationToken cancellationToken)
    {
        var link = await _accountCodeRepository.GetTafsilGroupLinkForUpdateAsync(
            request.AccountCodeId,
            request.LinkId,
            cancellationToken);

        if (link is null)
        {
            throw new NotFoundException("AccountTafsilGroupLink", request.LinkId);
        }

        if (link.ISDELETED)
        {
            return;
        }

        link.ISDELETED = true;
        link.CHANGEUSERID = _currentUser.UserId;
        link.UPDATEDDATE = DateTime.UtcNow;

        await _levelLinkSynchronizer.SyncAsync(request.AccountCodeId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
