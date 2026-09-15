using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Accounts.Commands.LinkAccountCodeToTafsilGroup;

/// <summary>
/// Constructs the <see cref="TB_ACCOUNT_LINK_TAFSILGROUP"/> Domain entity from the command,
/// stages it via <see cref="IAccountCodeRepository.AddTafsilGroupLinkAsync"/>, and owns the
/// transaction boundary by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once.
/// <c>ADDUSERID</c> is sourced from <see cref="ICurrentUser"/> — never from the request.
///
/// Deliberately does NOT check that <see cref="LinkAccountCodeToTafsilGroupCommand.AccountCodeId"/>
/// exists/is-not-deleted before staging the insert — the real FK (<c>FK_TAFSILGOUP_ACCOUNTCODE</c>)
/// already guarantees referential integrity, and pre-checking would just be a redundant
/// round-trip; an invalid id surfaces as the standard 400 FK-violation mapping.
/// </summary>
public sealed class LinkAccountCodeToTafsilGroupCommandHandler
    : IRequestHandler<LinkAccountCodeToTafsilGroupCommand, Guid>
{
    private readonly IAccountCodeRepository _accountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public LinkAccountCodeToTafsilGroupCommandHandler(
        IAccountCodeRepository accountCodeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountCodeRepository = accountCodeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(LinkAccountCodeToTafsilGroupCommand request, CancellationToken cancellationToken)
    {
        var link = new TB_ACCOUNT_LINK_TAFSILGROUP
        {
            ID = Guid.NewGuid(),
            ACCOUNT_ID = request.AccountCodeId,
            LEVEL_ID = request.LevelId,
            TAFSILGROUP_ID = request.TafsilGroupId,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _accountCodeRepository.AddTafsilGroupLinkAsync(link, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return link.ID;
    }
}
