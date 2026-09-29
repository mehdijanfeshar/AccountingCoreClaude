using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.SubmitPaymentRequest;

public sealed class SubmitPaymentRequestCommandHandler : IRequestHandler<SubmitPaymentRequestCommand>
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly IPaymentRequestEventRepository _eventRepository;
    private readonly IPaymentRequestSubmitRuleChecker _submitRuleChecker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public SubmitPaymentRequestCommandHandler(
        IPaymentRequestRepository paymentRequestRepository,
        IPaymentRequestEventRepository eventRepository,
        IPaymentRequestSubmitRuleChecker submitRuleChecker,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _paymentRequestRepository = paymentRequestRepository;
        _eventRepository = eventRepository;
        _submitRuleChecker = submitRuleChecker;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task Handle(SubmitPaymentRequestCommand request, CancellationToken cancellationToken)
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

        await _submitRuleChecker.EnsureSubmittableAsync(
            paymentRequest.ID,
            paymentRequest.BENEFICIARY_NATIONAL_ID,
            paymentRequest.INVOICE_REF,
            paymentRequest.INVOICE_APPROVED,
            paymentRequest.DUE_DATE,
            request.VahedCode,
            excludeId: paymentRequest.ID,
            cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var fromState = paymentRequest.REQUEST_STATE;

        paymentRequest.REQUEST_STATE = PaymentRequestState.PendingUnitManager;
        paymentRequest.SUBMITTED_DATE = now;
        paymentRequest.CHANGEUSERID = userId;
        paymentRequest.UPDATEDDATE = now;

        await _eventRepository.AddAsync(
            new TB_TR_PAYMENT_REQUEST_EVENT
            {
                ID = Guid.NewGuid(),
                PAYMENT_REQUEST_ID = paymentRequest.ID,
                ACTION = PaymentRequestEventAction.Submit,
                FROM_STATE = fromState,
                TO_STATE = PaymentRequestState.PendingUnitManager,
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
}
