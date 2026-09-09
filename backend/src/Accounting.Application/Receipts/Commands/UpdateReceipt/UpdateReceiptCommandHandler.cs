using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Receipts.Commands.UpdateReceipt;

/// <summary>
/// Loads the existing <see cref="Accounting.Domain.Entity.TB_RECEIP"/> row via
/// <see cref="IReceiptRepository.GetForUpdateAsync"/> (change-tracked), overwrites every writable
/// field from the command, stamps audit columns, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. Throws
/// <see cref="NotFoundException"/> — mapped to 404 by <c>GlobalExceptionHandler</c> — when the
/// row does not exist or is already soft-deleted (<c>ISDELETED</c> is non-nullable <c>bool</c> on
/// this table). <c>CHANGEUSERID</c> is sourced from <see cref="ICurrentUser"/> — never from the
/// request.
///
/// <c>request.VahedCode</c> is likewise unforgeable, enforced one layer earlier: by the time
/// this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler simply maps it onto
/// <c>TB_RECEIP.VAHEDCODE</c> at face value — see <c>UpdateReceiptCommand</c> XML doc for the
/// explicit scope note on what this does and does not cover.
/// </summary>
public sealed class UpdateReceiptCommandHandler : IRequestHandler<UpdateReceiptCommand>
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateReceiptCommandHandler(
        IReceiptRepository receiptRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _receiptRepository = receiptRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateReceiptCommand request, CancellationToken cancellationToken)
    {
        var entity = await _receiptRepository.GetForUpdateAsync(request.Id, cancellationToken);

        if (entity is null || entity.ISDELETED)
        {
            throw new NotFoundException("Receipt", request.Id);
        }

        entity.RECEIPT_KIND = request.ReceiptKind;
        entity.RECEIPT_DATE = request.ReceiptDate;
        entity.RECEIPT_NO = request.ReceiptNo;
        entity.DATE_RSID = request.DateRsid;
        entity.VAHEDCODE = request.VahedCode;
        entity.YEAR = request.Year;
        entity.CHANGEUSERID = _currentUser.UserId;
        entity.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
