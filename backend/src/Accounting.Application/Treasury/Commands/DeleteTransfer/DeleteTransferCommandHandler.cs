using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.DeleteTransfer;

public sealed class DeleteTransferCommandHandler : IRequestHandler<DeleteTransferCommand>
{
    private readonly ITreasuryTransferRepository _transferRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteTransferCommandHandler(
        ITreasuryTransferRepository transferRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _transferRepository = transferRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteTransferCommand request, CancellationToken cancellationToken)
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

        transfer.ISDELETED = true;
        transfer.CHANGEUSERID = _currentUser.UserId;
        transfer.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
