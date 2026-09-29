using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ResumePaymentRequest;

public sealed class ResumePaymentRequestCommandHandler : IRequestHandler<ResumePaymentRequestCommand>
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly IPaymentRequestEventRepository _eventRepository;
    private readonly IPaymentRequestExecutionAuthorizer _executionAuthorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public ResumePaymentRequestCommandHandler(
        IPaymentRequestRepository paymentRequestRepository,
        IPaymentRequestEventRepository eventRepository,
        IPaymentRequestExecutionAuthorizer executionAuthorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _paymentRequestRepository = paymentRequestRepository;
        _eventRepository = eventRepository;
        _executionAuthorizer = executionAuthorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task Handle(ResumePaymentRequestCommand request, CancellationToken cancellationToken)
    {
        var paymentRequest = await _paymentRequestRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (paymentRequest is null || paymentRequest.ISDELETED)
        {
            throw new NotFoundException("PaymentRequest", request.Id);
        }

        if (paymentRequest.REQUEST_STATE != PaymentRequestState.Suspended)
        {
            throw new PaymentRequestStateConflictException(request.Id, paymentRequest.REQUEST_STATE, "معلق");
        }

        await _executionAuthorizer.EnsureTreasurerAsync(request.Id, request.VahedCode, cancellationToken);

        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        paymentRequest.REQUEST_STATE = PaymentRequestState.ReadyForExecution;
        paymentRequest.CHANGEUSERID = userId;
        paymentRequest.UPDATEDDATE = now;

        await _eventRepository.AddAsync(
            new TB_TR_PAYMENT_REQUEST_EVENT
            {
                ID = Guid.NewGuid(),
                PAYMENT_REQUEST_ID = paymentRequest.ID,
                ACTION = PaymentRequestEventAction.Resume,
                FROM_STATE = PaymentRequestState.Suspended,
                TO_STATE = PaymentRequestState.ReadyForExecution,
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
