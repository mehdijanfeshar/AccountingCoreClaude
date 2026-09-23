using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Commands.ReactivateWhiteAndBlackList;

/// <summary>
/// Loads the row change-tracked, writes the requested state and date range, stamps the audit
/// columns and saves once. Throws <see cref="NotFoundException"/> — mapped to 404 — when the row
/// does not exist or is soft-deleted.
///
/// The date pair the caller did NOT choose is explicitly nulled rather than left alone. That
/// matters: a row reactivated as <see cref="WhiteBlackListState.SystemOnly"/> after previously
/// being <see cref="WhiteBlackListState.Allowed"/> would otherwise keep a stale authorized window
/// alongside its new restriction window, and the grid would render both.
/// </summary>
public sealed class ReactivateWhiteAndBlackListCommandHandler : IRequestHandler<ReactivateWhiteAndBlackListCommand>
{
    private readonly IWhiteAndBlackListRepository _whiteAndBlackListRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ReactivateWhiteAndBlackListCommandHandler(
        IWhiteAndBlackListRepository whiteAndBlackListRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _whiteAndBlackListRepository = whiteAndBlackListRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(ReactivateWhiteAndBlackListCommand request, CancellationToken cancellationToken)
    {
        var entity = await _whiteAndBlackListRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("WhiteAndBlackList", request.Id);
        }

        var isAllowed = request.State == WhiteBlackListState.Allowed;

        entity.STATE = request.State;
        entity.FROMAUTHORIZEDDATE = isAllowed ? request.FromDate : null;
        entity.TOAUTHORIZEDDATE = isAllowed ? request.ToDate : null;
        entity.FROMLIMITATIONDATE = isAllowed ? null : request.FromDate;
        entity.TOLIMITATIONDATE = isAllowed ? null : request.ToDate;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
