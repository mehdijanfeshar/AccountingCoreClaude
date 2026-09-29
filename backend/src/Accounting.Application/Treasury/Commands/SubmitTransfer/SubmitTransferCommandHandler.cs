using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.SubmitTransfer;

public sealed class SubmitTransferCommandHandler : IRequestHandler<SubmitTransferCommand>
{
    private readonly ITreasuryTransferRepository _transferRepository;
    private readonly ITreasuryTransferEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public SubmitTransferCommandHandler(
        ITreasuryTransferRepository transferRepository,
        ITreasuryTransferEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _transferRepository = transferRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task Handle(SubmitTransferCommand request, CancellationToken cancellationToken)
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

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var fromState = transfer.STATE;

        transfer.STATE = TransferState.PendingTreasurer;
        transfer.CHANGEUSERID = userId;
        transfer.UPDATEDDATE = now;

        await _eventRepository.AddAsync(
            new TB_TR_TRANSFER_EVENT
            {
                ID = Guid.NewGuid(),
                TRANSFER_ID = transfer.ID,
                ACTION = TransferEventAction.Submit,
                FROM_STATE = fromState,
                TO_STATE = TransferState.PendingTreasurer,
                CLIENT_IP = _clientInfoProvider.ClientIp,
                VAHEDCODE = request.VahedCode,
                YEAR = transfer.YEAR,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
