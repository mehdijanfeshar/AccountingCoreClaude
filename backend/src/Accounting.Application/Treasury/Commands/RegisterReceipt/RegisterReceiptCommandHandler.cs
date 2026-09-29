using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using MediatR;

namespace Accounting.Application.Treasury.Commands.RegisterReceipt;

/// <summary>
/// Sensitive multi-write use case (GL voucher + Legacy PayReciv head/detail + state transition) —
/// wrapped in an explicit <see cref="IUnitOfWork.BeginTransactionAsync"/>/<see cref="IUnitOfWork.CommitTransactionAsync"/>
/// pair per CLAUDE.md's rule for سند/تولید شماره use cases, same shape as
/// <c>ExecutePaymentRequestCommandHandler</c>/<c>CreateReceiptCommandHandler</c>'s register path.
/// </summary>
public sealed class RegisterReceiptCommandHandler : IRequestHandler<RegisterReceiptCommand>
{
    private readonly ITreasuryReceiptRepository _receiptRepository;
    private readonly IReceiptRegistrationService _registrationService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterReceiptCommandHandler(
        ITreasuryReceiptRepository receiptRepository,
        IReceiptRegistrationService registrationService,
        IUnitOfWork unitOfWork)
    {
        _receiptRepository = receiptRepository;
        _registrationService = registrationService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RegisterReceiptCommand request, CancellationToken cancellationToken)
    {
        var receipt = await _receiptRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (receipt is null || receipt.ISDELETED)
        {
            throw new NotFoundException("Receipt", request.Id);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _registrationService.RegisterAsync(receipt, request.VahedCode, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
