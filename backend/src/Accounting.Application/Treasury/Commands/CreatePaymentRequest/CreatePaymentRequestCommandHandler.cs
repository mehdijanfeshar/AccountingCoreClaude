using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.CreatePaymentRequest;

/// <summary>
/// Builds one <see cref="TB_TR_PAYMENT_REQUEST"/> + one <see cref="TB_TR_PAYMENT_REQUEST_EVENT"/>
/// and persists both with a single <see cref="IUnitOfWork.SaveChangesAsync"/> call — same shape as
/// <c>CreatePettyCashExpenseDocCommandHandler</c>.
/// </summary>
public sealed class CreatePaymentRequestCommandHandler : IRequestHandler<CreatePaymentRequestCommand, Guid>
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly IPaymentRequestEventRepository _eventRepository;
    private readonly IAccountCodeReadRepository _accountCodeReadRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly IPaymentRequestSubmitRuleChecker _submitRuleChecker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public CreatePaymentRequestCommandHandler(
        IPaymentRequestRepository paymentRequestRepository,
        IPaymentRequestEventRepository eventRepository,
        IAccountCodeReadRepository accountCodeReadRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        IPaymentRequestSubmitRuleChecker submitRuleChecker,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _paymentRequestRepository = paymentRequestRepository;
        _eventRepository = eventRepository;
        _accountCodeReadRepository = accountCodeReadRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _submitRuleChecker = submitRuleChecker;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task<Guid> Handle(CreatePaymentRequestCommand request, CancellationToken cancellationToken)
    {
        _ = await _accountCodeReadRepository.GetByIdAsync(request.ExpenseAccountId, cancellationToken)
            ?? throw new NotFoundException("AccountCode", request.ExpenseAccountId);

        _ = await _bankAccountReadRepository.GetByIdAsync(request.PaymentAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.PaymentAccountId);

        var id = Guid.NewGuid();

        var (vatAmount, insuranceDeductionAmount, netPayableAmount) = PaymentRequestAmountCalculator.Compute(
            request.AmountBeforeTax,
            request.VatPercent,
            request.VatAmount,
            request.InsuranceDeductionPercent,
            request.InsuranceDeductionAmount);

        var initialState = request.Submit ? PaymentRequestState.PendingUnitManager : PaymentRequestState.Draft;

        if (request.Submit)
        {
            await _submitRuleChecker.EnsureSubmittableAsync(
                id,
                request.BeneficiaryNationalId,
                request.InvoiceRef,
                request.InvoiceApproved,
                request.DueDate,
                request.VahedCode,
                excludeId: null,
                cancellationToken);
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var nextCode = await _paymentRequestRepository.GetNextCodeAsync(request.VahedCode, request.Year, cancellationToken);

        var paymentRequest = new TB_TR_PAYMENT_REQUEST
        {
            ID = id,
            CODE = "PAY-" + nextCode.ToString("000000"),
            BENEFICIARY_NAME = request.BeneficiaryName,
            BENEFICIARY_NATIONAL_ID = request.BeneficiaryNationalId,
            BENEFICIARY_TAFSILI_ID = request.BeneficiaryTafsiliId,
            PAYMENT_TYPE = request.PaymentType,
            INVOICE_REF = request.InvoiceRef,
            INVOICE_APPROVED = request.InvoiceApproved,
            EXPENSE_ACCOUNT_ID = request.ExpenseAccountId,
            COST_CENTER_TAFSILI_ID = request.CostCenterTafsiliId,
            AMOUNT_BEFORE_TAX = request.AmountBeforeTax,
            VAT_PERCENT = request.VatPercent,
            VAT_AMOUNT = vatAmount,
            INSURANCE_DEDUCTION_PERCENT = request.InsuranceDeductionPercent,
            INSURANCE_DEDUCTION_AMOUNT = insuranceDeductionAmount,
            NET_PAYABLE_AMOUNT = netPayableAmount,
            DUE_DATE = request.DueDate,
            PAYMENT_ACCOUNT_ID = request.PaymentAccountId,
            PAYMENT_METHOD = request.PaymentMethod,
            DESCRIPTION = request.Description,
            REQUEST_STATE = initialState,
            SUBMITTED_DATE = request.Submit ? now : null,
            VAHEDCODE = request.VahedCode,
            YEAR = request.Year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _paymentRequestRepository.AddAsync(paymentRequest, cancellationToken);

        await _eventRepository.AddAsync(
            new TB_TR_PAYMENT_REQUEST_EVENT
            {
                ID = Guid.NewGuid(),
                PAYMENT_REQUEST_ID = id,
                ACTION = PaymentRequestEventAction.Create,
                FROM_STATE = null,
                TO_STATE = initialState,
                CLIENT_IP = _clientInfoProvider.ClientIp,
                VAHEDCODE = request.VahedCode,
                YEAR = request.Year,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return id;
    }
}
