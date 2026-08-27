using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Commands.UpdateWhiteAndBlackList;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_WHITEANDBLACKLIST"/> row via
/// <see cref="IWhiteAndBlackListRepository.GetForUpdateAsync"/> (change-tracked), overwrites
/// every writable field from the command, stamps audit columns, and owns the transaction
/// boundary by calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted. <c>ISDELETED</c> is <c>bool?</c> on this
/// table, so both <see langword="false"/> and <see langword="null"/> are treated as
/// "not deleted" — only an explicit <see langword="true"/> triggers 404, consistent with the
/// <c>ISDELETED != true</c> filter used by the read side. <c>CHANGEUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
/// </summary>
public sealed class UpdateWhiteAndBlackListCommandHandler : IRequestHandler<UpdateWhiteAndBlackListCommand>
{
    private readonly IWhiteAndBlackListRepository _whiteAndBlackListRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateWhiteAndBlackListCommandHandler(
        IWhiteAndBlackListRepository whiteAndBlackListRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _whiteAndBlackListRepository = whiteAndBlackListRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateWhiteAndBlackListCommand request, CancellationToken cancellationToken)
    {
        var entity = await _whiteAndBlackListRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("WhiteAndBlackList", request.Id);
        }

        entity.ACCOUNTCODE_ID = request.AccountCodeId;
        entity.VAHEDTYPE_ID = request.VahedTypeId;
        entity.FROMAUTHORIZEDDATE = request.FromAuthorizedDate;
        entity.TOAUTHORIZEDDATE = request.ToAuthorizedDate;
        entity.FROMLIMITATIONDATE = request.FromLimitationDate;
        entity.TOLIMITATIONDATE = request.ToLimitationDate;
        entity.STATE = request.State;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
