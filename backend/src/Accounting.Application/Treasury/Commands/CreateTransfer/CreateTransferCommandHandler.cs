using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CreateTransfer;

public sealed class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, Guid>
{
    private readonly ITreasuryTransferRepository _transferRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateTransferCommandHandler(
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

    public async Task<Guid> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        _ = await _bankAccountReadRepository.GetByIdAsync(request.SourceBankAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.SourceBankAccountId);

        _ = await _bankAccountReadRepository.GetByIdAsync(request.DestBankAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.DestBankAccountId);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var nextCode = await _transferRepository.GetNextCodeAsync(request.VahedCode, request.Year, cancellationToken);

        var transfer = new TB_TR_TRANSFER
        {
            ID = Guid.NewGuid(),
            CODE = "TRF-" + nextCode.ToString("000000"),
            SOURCE_BANK_ACCOUNT_ID = request.SourceBankAccountId,
            DEST_BANK_ACCOUNT_ID = request.DestBankAccountId,
            AMOUNT = request.Amount,
            TRANSFER_DATE = request.TransferDate,
            TRANSFER_METHOD = request.TransferMethod,
            REASON = request.Reason,
            STATE = TransferState.Draft,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _transferRepository.AddAsync(transfer, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return transfer.ID;
    }
}
