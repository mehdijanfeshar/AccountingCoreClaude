using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpdateTransfer;

public sealed class UpdateTransferCommandHandler : IRequestHandler<UpdateTransferCommand>
{
    private readonly ITreasuryTransferRepository _transferRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateTransferCommandHandler(
        ITreasuryTransferRepository transferRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _transferRepository = transferRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await _transferRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (transfer is null || transfer.ISDELETED)
        {
            throw new NotFoundException("Transfer", request.Id);
        }

        if (transfer.STATE is not (TransferState.Draft or TransferState.Returned))
        {
            throw new TreasuryTransferStateConflictException(request.Id, transfer.STATE, "پیش‌نویس یا برگشتی");
        }

        _ = await _bankAccountReadRepository.GetByIdAsync(request.SourceBankAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.SourceBankAccountId);

        _ = await _bankAccountReadRepository.GetByIdAsync(request.DestBankAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.DestBankAccountId);

        transfer.SOURCE_BANK_ACCOUNT_ID = request.SourceBankAccountId;
        transfer.DEST_BANK_ACCOUNT_ID = request.DestBankAccountId;
        transfer.AMOUNT = request.Amount;
        transfer.TRANSFER_DATE = request.TransferDate;
        transfer.TRANSFER_METHOD = request.TransferMethod;
        transfer.REASON = request.Reason;
        transfer.CHANGEUSERID = _currentUser.UserId;
        transfer.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
