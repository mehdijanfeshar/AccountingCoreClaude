using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PayReciveHeads.Commands.UpdatePayReciveHead;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_PAYRECIVHEAD"/> row via
/// <see cref="IPayReciveHeadRepository.GetForUpdateAsync"/> (change-tracked), overwrites every
/// writable field from the command, stamps audit columns, and owns the transaction boundary by
/// calling <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted.
///
/// <c>ISDELETED</c> is a <b>non-nullable</b> <c>bool</c> on this table (unlike
/// <c>TB_TMP_VOUCHERHEAD</c>, <c>TB_ELAMHEAD</c> or <c>TB_RABET</c>), so the plain
/// <c>entity.ISDELETED</c> test below is correct and complete — there is no <c>null</c> branch to
/// consider, and none should be added.
///
/// <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> (the authenticated caller) —
/// never from the request.
///
/// <c>request.VahedCode</c> is likewise unforgeable, enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler simply maps it onto
/// <c>TB_PAYRECIVHEAD.VAHEDCODE</c> at face value — see <c>UpdatePayReciveHeadCommand</c> XML
/// doc for the explicit scope note on what this does and does not cover.
///
/// ⚠️ HEAD ONLY — no <c>TB_PAYRECIVDETAIL</c> row is ever touched here. See
/// <see cref="UpdatePayReciveHeadCommand"/> XML doc.
/// </summary>
public sealed class UpdatePayReciveHeadCommandHandler : IRequestHandler<UpdatePayReciveHeadCommand>
{
    private readonly IPayReciveHeadRepository _payReciveHeadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdatePayReciveHeadCommandHandler(
        IPayReciveHeadRepository payReciveHeadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _payReciveHeadRepository = payReciveHeadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdatePayReciveHeadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _payReciveHeadRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("PayReciveHead", request.Id);
        }

        entity.PAYRECIVCODE = request.PayReciveCode;
        entity.PAYRECIVDATE = request.PayReciveDate;
        entity.PAYRECIVDESCRIPTION = request.PayReciveDescription;
        entity.PAYRECIVTYPE = request.PayReciveType;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.VOUCHERSHEAD_ID = request.VoucherHeadId;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
