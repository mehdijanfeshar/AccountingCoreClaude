using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.RevolvingFunds.Commands.UpdateRevolvingFund;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_REVOLVING_FUND"/> row via
/// <see cref="IRevolvingFundRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted. <c>ISDELETED</c> is <c>bool?</c> on this
/// table, so both <see langword="false"/> and <see langword="null"/> are treated as
/// "not deleted" — only an explicit <see langword="true"/> triggers 404, consistent with the
/// <c>ISDELETED != true</c> filter used by the read side. <c>CHANGEUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
/// </summary>
public sealed class UpdateRevolvingFundCommandHandler : IRequestHandler<UpdateRevolvingFundCommand>
{
    private readonly IRevolvingFundRepository _revolvingFundRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateRevolvingFundCommandHandler(
        IRevolvingFundRepository revolvingFundRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _revolvingFundRepository = revolvingFundRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateRevolvingFundCommand request, CancellationToken cancellationToken)
    {
        var entity = await _revolvingFundRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED == true)
        {
            throw new NotFoundException("RevolvingFund", request.Id);
        }

        entity.CODE = request.Code;
        entity.NAME = request.Name;
        entity.DESCRIPTION = request.Description;
        entity.DEFAULTAMOUNT = request.DefaultAmount;
        entity.ACCOUNTCODE_ID = request.AccountCodeId;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
