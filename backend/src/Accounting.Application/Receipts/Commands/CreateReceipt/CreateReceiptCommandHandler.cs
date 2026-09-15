using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using MediatR;

namespace Accounting.Application.Receipts.Commands.CreateReceipt;

/// <summary>
/// Constructs the <see cref="TB_RECEIP"/> Domain entity from the command, stages it via
/// <see cref="IReceiptRepository"/>, and owns the transaction boundary by calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> exactly once. <c>ADDUSERID</c> is sourced from
/// <see cref="ICurrentUser"/> (the authenticated caller) — never from the request.
///
/// <c>ID</c> is always generated application-side (<see cref="Guid.NewGuid"/>).
///
/// <c>request.VahedCode</c> is equally unforgeable, just enforced one layer earlier: by the
/// time this handler runs, <c>VahedScopeBehavior</c> has already overwritten it with
/// <see cref="ICurrentUser.VahedCode"/>, so this handler can trust the field at face value and
/// simply map it onto <c>TB_RECEIP.VAHEDCODE</c> — it does not read <see cref="ICurrentUser"/>
/// directly for this field the way it does for <c>ADDUSERID</c>.
/// </summary>
public sealed class CreateReceiptCommandHandler : IRequestHandler<CreateReceiptCommand, Guid>
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateReceiptCommandHandler(
        IReceiptRepository receiptRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _receiptRepository = receiptRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateReceiptCommand request, CancellationToken cancellationToken)
    {
        var entity = new TB_RECEIP
        {
            ID = Guid.NewGuid(),
            RECEIPT_KIND = request.ReceiptKind,
            RECEIPT_DATE = request.ReceiptDate,
            RECEIPT_NO = request.ReceiptNo,
            DATE_RSID = request.DateRsid,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = _currentUser.UserId,
            CREATEDDATE = DateTime.UtcNow,
            ISDELETED = false,
        };

        await _receiptRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.ID;
    }
}
