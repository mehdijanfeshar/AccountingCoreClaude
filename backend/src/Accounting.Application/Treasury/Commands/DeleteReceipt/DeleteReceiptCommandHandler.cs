using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.DeleteReceipt;

public sealed class DeleteReceiptCommandHandler : IRequestHandler<DeleteReceiptCommand>
{
    private readonly ITreasuryReceiptRepository _receiptRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteReceiptCommandHandler(
        ITreasuryReceiptRepository receiptRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _receiptRepository = receiptRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteReceiptCommand request, CancellationToken cancellationToken)
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

        var now = DateTime.UtcNow;

        receipt.ISDELETED = true;
        receipt.CHANGEUSERID = _currentUser.UserId;
        receipt.UPDATEDDATE = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
