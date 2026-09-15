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
/// Idempotency: HTTP DELETE is defined as idempotent, so a link that is already soft-deleted is
/// treated as a no-op success rather than a 404 — no re-stamp, and
/// <see cref="IUnitOfWork.SaveChangesAsync"/> is skipped entirely.
/// </summary>
public sealed class UnlinkAccountCodeFromTafsilGroupCommandHandler : IRequestHandler<UnlinkAccountCodeFromTafsilGroupCommand>
{
    private readonly IAccountCodeRepository _accountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UnlinkAccountCodeFromTafsilGroupCommandHandler(
        IAccountCodeRepository accountCodeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountCodeRepository = accountCodeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
