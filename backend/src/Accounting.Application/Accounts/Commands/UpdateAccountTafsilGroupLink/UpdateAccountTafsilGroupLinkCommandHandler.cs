using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Accounts.Commands.UpdateAccountTafsilGroupLink;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_ACCOUNT_LINK_TAFSILGROUP"/> row via
/// <see cref="IAccountCodeRepository.GetTafsilGroupLinkForUpdateAsync"/> (change-tracked, scoped
/// to the given معین), overwrites <c>LEVEL_ID</c>/<c>TAFSILGROUP_ID</c>, stamps audit columns, and
/// owns the transaction boundary via <see cref="IUnitOfWork.SaveChangesAsync"/>. Throws
/// <see cref="NotFoundException"/> — mapped to 404 — when no such link exists under that معین or
/// it is already soft-deleted (<c>ISDELETED</c> is non-nullable <c>bool</c> on this table).
/// </summary>
public sealed class UpdateAccountTafsilGroupLinkCommandHandler : IRequestHandler<UpdateAccountTafsilGroupLinkCommand>
{
    private readonly IAccountCodeRepository _accountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateAccountTafsilGroupLinkCommandHandler(
        IAccountCodeRepository accountCodeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _accountCodeRepository = accountCodeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateAccountTafsilGroupLinkCommand request, CancellationToken cancellationToken)
    {
        var link = await _accountCodeRepository.GetTafsilGroupLinkForUpdateAsync(
            request.AccountCodeId,
            request.LinkId,
            cancellationToken);

        if (link is null || link.ISDELETED)
        {
            throw new NotFoundException("AccountTafsilGroupLink", request.LinkId);
        }

        link.LEVEL_ID = request.LevelId;
        link.TAFSILGROUP_ID = request.TafsilGroupId;
        link.CHANGEUSERID = _currentUser.UserId;
        link.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
