using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.UpdatePaymentRequest;

public sealed class UpdatePaymentRequestCommandHandler : IRequestHandler<UpdatePaymentRequestCommand>
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly IPaymentRequestEventRepository _eventRepository;
    private readonly IAccountCodeReadRepository _accountCodeReadRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly IPaymentRequestTafsiliValidator _tafsiliValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public UpdatePaymentRequestCommandHandler(
        IPaymentRequestRepository paymentRequestRepository,
        IPaymentRequestEventRepository eventRepository,
        IAccountCodeReadRepository accountCodeReadRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        IPaymentRequestTafsiliValidator tafsiliValidator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _paymentRequestRepository = paymentRequestRepository;
        _eventRepository = eventRepository;
        _accountCodeReadRepository = accountCodeReadRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _tafsiliValidator = tafsiliValidator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task Handle(UpdatePaymentRequestCommand request, CancellationToken cancellationToken)
    {
        var paymentRequest = await _paymentRequestRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (paymentRequest is null || paymentRequest.ISDELETED)
        {
            throw new NotFoundException("PaymentRequest", request.Id);
        }

        if (paymentRequest.REQUEST_STATE is not (PaymentRequestState.Draft or PaymentRequestState.Returned))
        {
            throw new PaymentRequestStateConflictException(request.Id, paymentRequest.REQUEST_STATE, "پیش‌نویس یا برگشتی");
        }

        if (!string.Equals(_currentUser.UserId, paymentRequest.ADDUSERID, StringComparison.Ordinal))
        {
            throw new PaymentRequestNotCreatorException(request.Id);
        }

        _ = await _accountCodeReadRepository.GetByIdAsync(request.ExpenseAccountId, cancellationToken)
            ?? throw new NotFoundException("AccountCode", request.ExpenseAccountId);

        _ = await _bankAccountReadRepository.GetByIdAsync(request.PaymentAccountId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("BankAccount", request.PaymentAccountId);

        await _tafsiliValidator.EnsureCostCenterTafsilisValidAsync(
            request.ExpenseAccountId, request.CostCenterTafsilis, request.VahedCode, cancellationToken);

        await _tafsiliValidator.EnsureBeneficiaryTafsiliValidAsync(
            request.BeneficiaryTafsiliId, request.VahedCode, cancellationToken);

        var (vatAmount, insuranceDeductionAmount, netPayableAmount) = PaymentRequestAmountCalculator.Compute(
            request.AmountBeforeTax,
            request.VatPercent,
            request.VatAmount,
            request.InsuranceDeductionPercent,
            request.InsuranceDeductionAmount);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        paymentRequest.BENEFICIARY_NAME = request.BeneficiaryName;
        paymentRequest.BENEFICIARY_NATIONAL_ID = request.BeneficiaryNationalId;
        paymentRequest.BENEFICIARY_TAFSILI_ID = request.BeneficiaryTafsiliId;
        paymentRequest.PAYMENT_TYPE = request.PaymentType;
        paymentRequest.INVOICE_REF = request.InvoiceRef;
        paymentRequest.INVOICE_APPROVED = request.InvoiceApproved;
        paymentRequest.EXPENSE_ACCOUNT_ID = request.ExpenseAccountId;
        paymentRequest.AMOUNT_BEFORE_TAX = request.AmountBeforeTax;
        paymentRequest.VAT_PERCENT = request.VatPercent;
        paymentRequest.VAT_AMOUNT = vatAmount;
        paymentRequest.INSURANCE_DEDUCTION_PERCENT = request.InsuranceDeductionPercent;
        paymentRequest.INSURANCE_DEDUCTION_AMOUNT = insuranceDeductionAmount;
        paymentRequest.NET_PAYABLE_AMOUNT = netPayableAmount;
        paymentRequest.DUE_DATE = request.DueDate;
        paymentRequest.PAYMENT_ACCOUNT_ID = request.PaymentAccountId;
        paymentRequest.PAYMENT_METHOD = request.PaymentMethod;
        paymentRequest.DESCRIPTION = request.Description;
        paymentRequest.CHANGEUSERID = userId;
        paymentRequest.UPDATEDDATE = now;

        await ReconcileCostCenterTafsilisAsync(
            paymentRequest.ID, request.CostCenterTafsilis, request.VahedCode, userId, now, cancellationToken);

        await _eventRepository.AddAsync(
            new TB_TR_PAYMENT_REQUEST_EVENT
            {
                ID = Guid.NewGuid(),
                PAYMENT_REQUEST_ID = paymentRequest.ID,
                ACTION = PaymentRequestEventAction.Update,
                FROM_STATE = paymentRequest.REQUEST_STATE,
                TO_STATE = paymentRequest.REQUEST_STATE,
                CLIENT_IP = _clientInfoProvider.ClientIp,
                VAHEDCODE = request.VahedCode,
                YEAR = paymentRequest.YEAR,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Full-replace reconcile of <c>TB_TR_PAYMENT_REQUEST_LINK_TAFSILI</c> against
    /// <paramref name="requested"/>, matching existing rows on the (TafsiliId, LevelId) pair — same
    /// reconcile shape as <c>UpsertPettyCashFundTafsilisCommandHandler</c>. Surviving rows are left
    /// untouched (not re-stamped).
    /// </summary>
    private async Task ReconcileCostCenterTafsilisAsync(
        Guid paymentRequestId,
        IReadOnlyList<PaymentRequestTafsiliLinkInput> requested,
        string vahedCode,
        string userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await _paymentRequestRepository.GetActiveCostCenterTafsiliLinksAsync(paymentRequestId, cancellationToken);

        var requestedKeys = requested.Select(l => (l.TafsiliId, l.LevelId)).ToHashSet();

        foreach (var link in existing)
        {
            if (requestedKeys.Contains((link.TAFSILI_ID, link.LEVEL_ID)))
            {
                continue;
            }

            link.ISDELETED = true;
            link.CHANGEUSERID = userId;
            link.UPDATEDDATE = now;
        }

        var existingKeys = existing.Select(l => (l.TAFSILI_ID, l.LEVEL_ID)).ToHashSet();

        foreach (var link in requested)
        {
            if (existingKeys.Contains((link.TafsiliId, link.LevelId)))
            {
                continue;
            }

            await _paymentRequestRepository.AddCostCenterTafsiliLinkAsync(
                new TB_TR_PAYMENT_REQUEST_LINK_TAFSILI
                {
                    ID = Guid.NewGuid(),
                    PAYMENT_REQUEST_ID = paymentRequestId,
                    TAFSILI_ID = link.TafsiliId,
                    LEVEL_ID = link.LevelId,
                    VAHEDCODE = vahedCode,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                },
                cancellationToken);
        }
    }
}
