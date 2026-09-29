using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CreateReceipt;

/// <summary>
/// Builds one <see cref="TB_TR_RECEIPT"/> as پیش‌نویس — and, when <see cref="CreateReceiptCommand.Register"/>
/// is true, immediately registers it in the same call (same shortcut شکل as
/// <c>CreatePaymentRequestCommandHandler</c>'s <c>Submit</c>). The Register path is a سند + Legacy
/// PayReciv write, so it gets an explicit <see cref="IUnitOfWork.BeginTransactionAsync"/> boundary
/// (CLAUDE.md rule for سند/تولید‌شماره use cases); the plain-draft path does not need one (a
/// single-row insert).
/// </summary>
public sealed class CreateReceiptCommandHandler : IRequestHandler<CreateReceiptCommand, Guid>
{
    private readonly ITreasuryReceiptRepository _receiptRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly IReceiptPayerValidator _payerValidator;
    private readonly IReceiptRegistrationService _registrationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateReceiptCommandHandler(
        ITreasuryReceiptRepository receiptRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        IReceiptPayerValidator payerValidator,
        IReceiptRegistrationService registrationService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _receiptRepository = receiptRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _payerValidator = payerValidator;
        _registrationService = registrationService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateReceiptCommand request, CancellationToken cancellationToken)
    {
        _ = await _bankAccountReadRepository.GetByIdAsync(request.BankAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.BankAccountId);

        var payerName = await _payerValidator.EnsurePayerValidAsync(request.PayerTafsiliId, request.VahedCode, cancellationToken);

        var duplicate = await _receiptRepository.ExistsDuplicateBankReferenceAsync(
            request.BankAccountId, request.BankReference, excludeId: null, cancellationToken);

        if (duplicate)
        {
            throw new TreasuryReceiptDuplicateBankReferenceException(request.BankReference, request.BankAccountId);
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var nextCode = await _receiptRepository.GetNextCodeAsync(request.VahedCode, request.Year, cancellationToken);

        var receipt = new TB_TR_RECEIPT
        {
            ID = Guid.NewGuid(),
            CODE = "RCV-" + nextCode.ToString("000000"),
            PAYER_TAFSILI_ID = request.PayerTafsiliId,
            PAYER_NAME = payerName,
            AMOUNT = request.Amount,
            BANK_ACCOUNT_ID = request.BankAccountId,
            RECEIPT_METHOD = request.ReceiptMethod,
            RECEIPT_DATE = request.ReceiptDate,
            BANK_REFERENCE = request.BankReference,
            INVOICE_REF = request.InvoiceRef,
            DESCRIPTION = request.Description,
            STATE = ReceiptState.Draft,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _receiptRepository.AddAsync(receipt, cancellationToken);

        if (!request.Register)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return receipt.ID;
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

        return receipt.ID;
    }
}
