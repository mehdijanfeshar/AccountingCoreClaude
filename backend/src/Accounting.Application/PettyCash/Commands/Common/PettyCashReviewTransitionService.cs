using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashReviewTransitionService : IPettyCashReviewTransitionService
{
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IPettyCashDocEventRepository _eventRepository;
    private readonly IPettyCashReviewAuthorizer _reviewAuthorizer;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public PettyCashReviewTransitionService(
        IPettyCashExpenseDocRepository expenseDocRepository,
        IChargeAndCostRepository chargeAndCostRepository,
        IPettyCashDocEventRepository eventRepository,
        IPettyCashReviewAuthorizer reviewAuthorizer,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _expenseDocRepository = expenseDocRepository;
        _chargeAndCostRepository = chargeAndCostRepository;
        _eventRepository = eventRepository;
        _reviewAuthorizer = reviewAuthorizer;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task<TB_PC_EXPENSE_DOC> TransitionAsync(
        Guid expenseDocId,
        string vahedCode,
        PettyCashDocState requiredFromState,
        PettyCashDocState toState,
        PettyCashDocAction action,
        string? note,
        string? returnReasonsCsv,
        string? returnDeadline,
        CancellationToken cancellationToken = default)
    {
        var doc = await _expenseDocRepository.GetForUpdateAsync(expenseDocId, vahedCode, cancellationToken);

        if (doc is null || doc.ISDELETED)
        {
            throw new NotFoundException("PettyCashExpenseDoc", expenseDocId);
        }

        // (الف) reviewer-of-this-fund check → 403, then (ب) SoD check → 409. Both before the
        // state check below, so a caller with no business reviewing this document at all never
        // learns anything about its current state.
        await _reviewAuthorizer.EnsureCanReviewAsync(doc.ID, doc.REVOLVINGFUND_ID, doc.ADDUSERID, cancellationToken);

        var fromState = doc.DOC_STATE;

        if (fromState != requiredFromState)
        {
            throw new PettyCashReviewStateConflictException(doc.ID, fromState, requiredFromState, action);
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        doc.DOC_STATE = toState;
        doc.CHANGEUSERID = userId;
        doc.UPDATEDDATE = now;

        if (action == PettyCashDocAction.Return)
        {
            doc.RETURN_DEADLINE = returnDeadline;
        }

        var head = await _chargeAndCostRepository.GetHeadForUpdateAsync(doc.CHARGEANDCOSTHEAD_ID, vahedCode, cancellationToken);

        if (head is not null)
        {
            head.STATUS = PettyCashStatusMap.ToLegacyStatus(toState);
            head.CHANGEUSERID = userId;
            head.UPDATEDDATE = now;
        }

        await _eventRepository.AddAsync(
            new TB_PC_DOC_EVENT
            {
                ID = Guid.NewGuid(),
                EXPENSE_DOC_ID = doc.ID,
                ACTION = action,
                FROM_STATE = fromState,
                TO_STATE = toState,
                NOTE = note,
                RETURN_REASONS = returnReasonsCsv,
                CLIENT_IP = _clientInfoProvider.ClientIp,
                VAHEDCODE = vahedCode,
                YEAR = doc.YEAR,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            },
            cancellationToken);

        return doc;
    }
}
