using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

public sealed class PaymentRequestApprovalService : IPaymentRequestApprovalService
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly IPaymentRequestEventRepository _eventRepository;
    private readonly ITreasurySettingReadRepository _settingReadRepository;
    private readonly ITreasuryRoleRepository _roleRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public PaymentRequestApprovalService(
        IPaymentRequestRepository paymentRequestRepository,
        IPaymentRequestEventRepository eventRepository,
        ITreasurySettingReadRepository settingReadRepository,
        ITreasuryRoleRepository roleRepository,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _paymentRequestRepository = paymentRequestRepository;
        _eventRepository = eventRepository;
        _settingReadRepository = settingReadRepository;
        _roleRepository = roleRepository;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task<TB_TR_PAYMENT_REQUEST> ApproveAsync(
        Guid id, string vahedCode, string? note, bool isBulkApprove, CancellationToken cancellationToken = default)
    {
        var request = await LoadPendingAsync(id, vahedCode, cancellationToken);

        var requiredRole = RequiredRoleForStage(request.REQUEST_STATE, id);

        await EnsureHasRoleAsync(vahedCode, id, requiredRole, cancellationToken);

        EnsureNotCreator(request, id);

        var lastApproveEvent = await _eventRepository.GetLastApproveEventAsync(id, cancellationToken);

        if (lastApproveEvent is not null && string.Equals(lastApproveEvent.ADDUSERID, _currentUser.UserId, StringComparison.Ordinal))
        {
            throw new PaymentRequestConsecutiveApproverConflictException(id);
        }

        var setting = await _settingReadRepository.GetByVahedAsync(vahedCode, cancellationToken)
            ?? throw new PaymentRequestSettingsMissingException(vahedCode);

        if (isBulkApprove && request.NET_PAYABLE_AMOUNT > setting.BulkApproveLimit)
        {
            throw new PaymentRequestBulkLimitExceededException(id, request.NET_PAYABLE_AMOUNT, setting.BulkApproveLimit);
        }

        var fromState = request.REQUEST_STATE;

        var toState = fromState switch
        {
            PaymentRequestState.PendingUnitManager => PaymentRequestState.PendingFinanceManager,
            PaymentRequestState.PendingFinanceManager => request.NET_PAYABLE_AMOUNT > setting.CeoApprovalThreshold
                ? PaymentRequestState.PendingCeo
                : PaymentRequestState.ReadyForExecution,
            PaymentRequestState.PendingCeo => PaymentRequestState.ReadyForExecution,
            _ => throw new PaymentRequestStateConflictException(id, fromState, "یکی از وضعیت‌های در انتظار تأیید"),
        };

        ApplyTransition(request, toState);

        await AddEventAsync(request, PaymentRequestEventAction.Approve, fromState, toState, note, vahedCode, cancellationToken);

        return request;
    }

    public async Task ReturnAsync(Guid id, string vahedCode, string reason, CancellationToken cancellationToken = default)
    {
        var request = await LoadPendingAsync(id, vahedCode, cancellationToken);

        var requiredRole = RequiredRoleForStage(request.REQUEST_STATE, id);

        await EnsureHasRoleAsync(vahedCode, id, requiredRole, cancellationToken);

        EnsureNotCreator(request, id);

        var fromState = request.REQUEST_STATE;

        ApplyTransition(request, PaymentRequestState.Returned);

        await AddEventAsync(request, PaymentRequestEventAction.Return, fromState, PaymentRequestState.Returned, reason, vahedCode, cancellationToken);
    }

    public async Task RejectAsync(Guid id, string vahedCode, string reason, CancellationToken cancellationToken = default)
    {
        var request = await LoadPendingAsync(id, vahedCode, cancellationToken);

        var requiredRole = RequiredRoleForStage(request.REQUEST_STATE, id);

        await EnsureHasRoleAsync(vahedCode, id, requiredRole, cancellationToken);

        EnsureNotCreator(request, id);

        var fromState = request.REQUEST_STATE;

        ApplyTransition(request, PaymentRequestState.Rejected);

        await AddEventAsync(request, PaymentRequestEventAction.Reject, fromState, PaymentRequestState.Rejected, reason, vahedCode, cancellationToken);
    }

    private async Task<TB_TR_PAYMENT_REQUEST> LoadPendingAsync(Guid id, string vahedCode, CancellationToken cancellationToken)
    {
        var request = await _paymentRequestRepository.GetForUpdateAsync(id, vahedCode, cancellationToken);

        if (request is null || request.ISDELETED)
        {
            throw new NotFoundException("PaymentRequest", id);
        }

        if (request.REQUEST_STATE is not (
            PaymentRequestState.PendingUnitManager or
            PaymentRequestState.PendingFinanceManager or
            PaymentRequestState.PendingCeo))
        {
            throw new PaymentRequestStateConflictException(id, request.REQUEST_STATE, "یکی از وضعیت‌های در انتظار تأیید");
        }

        return request;
    }

    /// <summary>Delegates to <see cref="PaymentRequestStageRoleMap"/> — see that type's XML doc for
    /// why this indirection exists.</summary>
    private static TreasuryRole RequiredRoleForStage(PaymentRequestState state, Guid id) =>
        PaymentRequestStageRoleMap.RequiredRoleForState(state)
            ?? throw new PaymentRequestStateConflictException(id, state, "یکی از وضعیت‌های در انتظار تأیید");

    /// <summary>
    /// Checked directly against <see cref="ITreasuryRoleRepository"/> (not
    /// <c>ITreasuryRoleAuthorizer</c>, which is the unit-wide admin gate for settings/roles CRUD
    /// and throws the differently-worded <see cref="TreasuryRoleRequiredException"/>) so a stage
    /// role failure here always carries the request-scoped
    /// <see cref="PaymentRequestRoleRequiredException"/> — a distinct message/exception type from
    /// the admin one, even though both are 403.
    /// </summary>
    private async Task EnsureHasRoleAsync(string vahedCode, Guid paymentRequestId, TreasuryRole requiredRole, CancellationToken cancellationToken)
    {
        var activeRoles = await _roleRepository.GetActiveRolesAsync(vahedCode, _currentUser.UserId, cancellationToken);

        if (!activeRoles.Contains(requiredRole))
        {
            throw new PaymentRequestRoleRequiredException(paymentRequestId, requiredRole);
        }
    }

    /// <summary>SoD: «تأییدکننده/برگشت‌دهنده/ردکننده ≠ ثبت‌کننده».</summary>
    private void EnsureNotCreator(TB_TR_PAYMENT_REQUEST request, Guid id)
    {
        if (string.Equals(request.ADDUSERID, _currentUser.UserId, StringComparison.Ordinal))
        {
            throw new PaymentRequestApproverConflictException(id);
        }
    }

    private void ApplyTransition(TB_TR_PAYMENT_REQUEST request, PaymentRequestState toState)
    {
        request.REQUEST_STATE = toState;
        request.CHANGEUSERID = _currentUser.UserId;
        request.UPDATEDDATE = DateTime.UtcNow;
    }

    private async Task AddEventAsync(
        TB_TR_PAYMENT_REQUEST request,
        PaymentRequestEventAction action,
        PaymentRequestState fromState,
        PaymentRequestState toState,
        string? note,
        string vahedCode,
        CancellationToken cancellationToken)
    {
        await _eventRepository.AddAsync(
            new TB_TR_PAYMENT_REQUEST_EVENT
            {
                ID = Guid.NewGuid(),
                PAYMENT_REQUEST_ID = request.ID,
                ACTION = action,
                FROM_STATE = fromState,
                TO_STATE = toState,
                NOTE = note,
                CLIENT_IP = _clientInfoProvider.ClientIp,
                VAHEDCODE = vahedCode,
                YEAR = request.YEAR,
                ADDUSERID = _currentUser.UserId,
                CREATEDDATE = DateTime.UtcNow,
                ISDELETED = false,
            },
            cancellationToken);
    }
}
