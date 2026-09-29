using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpdateReceipt;

public sealed class UpdateReceiptCommandHandler : IRequestHandler<UpdateReceiptCommand>
{
    private readonly ITreasuryReceiptRepository _receiptRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly IReceiptPayerValidator _payerValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateReceiptCommandHandler(
        ITreasuryReceiptRepository receiptRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        IReceiptPayerValidator payerValidator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _receiptRepository = receiptRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _payerValidator = payerValidator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateReceiptCommand request, CancellationToken cancellationToken)
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

        _ = await _bankAccountReadRepository.GetByIdAsync(request.BankAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.BankAccountId);

        var payerName = await _payerValidator.EnsurePayerValidAsync(request.PayerTafsiliId, request.VahedCode, cancellationToken);

        var duplicate = await _receiptRepository.ExistsDuplicateBankReferenceAsync(
            request.BankAccountId, request.BankReference, excludeId: request.Id, cancellationToken);

        if (duplicate)
        {
            throw new TreasuryReceiptDuplicateBankReferenceException(request.BankReference, request.BankAccountId);
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        receipt.PAYER_TAFSILI_ID = request.PayerTafsiliId;
        receipt.PAYER_NAME = payerName;
        receipt.AMOUNT = request.Amount;
        receipt.BANK_ACCOUNT_ID = request.BankAccountId;
        receipt.RECEIPT_METHOD = request.ReceiptMethod;
        receipt.RECEIPT_DATE = request.ReceiptDate;
        receipt.BANK_REFERENCE = request.BankReference;
        receipt.INVOICE_REF = request.InvoiceRef;
        receipt.DESCRIPTION = request.Description;
        receipt.CHANGEUSERID = userId;
        receipt.UPDATEDDATE = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
