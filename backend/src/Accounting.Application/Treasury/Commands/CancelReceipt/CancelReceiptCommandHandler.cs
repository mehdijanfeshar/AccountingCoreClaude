using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CancelReceipt;

public sealed class CancelReceiptCommandHandler : IRequestHandler<CancelReceiptCommand>
{
    private readonly ITreasuryReceiptRepository _receiptRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CancelReceiptCommandHandler(
        ITreasuryReceiptRepository receiptRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _receiptRepository = receiptRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(CancelReceiptCommand request, CancellationToken cancellationToken)
    {
        var receipt = await _receiptRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (receipt is null || receipt.ISDELETED)
        {
            throw new NotFoundException("Receipt", request.Id);
        }

        if (receipt.STATE != ReceiptState.Draft)
        {
            throw new TreasuryReceiptStateConflictException(request.Id, receipt.STATE, "پیش‌نویس");
        }

        receipt.STATE = ReceiptState.Cancelled;
        receipt.CHANGEUSERID = _currentUser.UserId;
        receipt.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
