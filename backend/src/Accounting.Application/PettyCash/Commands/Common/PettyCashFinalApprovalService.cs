using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashFinalApprovalService : IPettyCashFinalApprovalService
{
    private static readonly PettyCashRole[] FinalApprovalRoles =
    {
        PettyCashRole.FinanceManager,
        PettyCashRole.ChiefExecutive,
    };

    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IPettyCashFundRepository _fundRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IPettyCashDocEventRepository _eventRepository;
    private readonly IPettyCashReviewAuthorizer _reviewAuthorizer;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public PettyCashFinalApprovalService(
        IPettyCashExpenseDocRepository expenseDocRepository,
        IPettyCashFundRepository fundRepository,
        IChargeAndCostRepository chargeAndCostRepository,
        IPettyCashDocEventRepository eventRepository,
        IPettyCashReviewAuthorizer reviewAuthorizer,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _expenseDocRepository = expenseDocRepository;
        _fundRepository = fundRepository;
        _chargeAndCostRepository = chargeAndCostRepository;
        _eventRepository = eventRepository;
        _reviewAuthorizer = reviewAuthorizer;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task<TB_PC_EXPENSE_DOC> FinalApproveAsync(
        Guid expenseDocId,
        string vahedCode,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var doc = await _expenseDocRepository.GetForUpdateAsync(expenseDocId, vahedCode, cancellationToken);

        if (doc is null || doc.ISDELETED)
        {
            throw new NotFoundException("PettyCashExpenseDoc", expenseDocId);
        }

        // (الف)+(ب)+(پ): reviewer-of-this-fund-with-a-final-approval-capable-role check → 403,
        // then creator SoD → 409. Same order every other review action uses, before the state
        // check below.
        var callerRoles = await _reviewAuthorizer.EnsureCanReviewAsync(
            doc.ID, doc.FUND_ID, doc.ADDUSERID, FinalApprovalRoles, cancellationToken);

        var fromState = doc.DOC_STATE;

        if (fromState != PettyCashDocState.PendingReview)
        {
            throw new PettyCashReviewStateConflictException(
                doc.ID, fromState, PettyCashDocState.PendingReview, PettyCashDocAction.FinalApprove);
        }

        if (doc.VERIFIED_BY_USERID is null)
        {
            throw new PettyCashNotVerifiedException(doc.ID);
        }

        var userId = _currentUser.UserId;

        // Second SoD rule: the inspector who verified control cannot also be the final approver.
        if (string.Equals(userId, doc.VERIFIED_BY_USERID, StringComparison.Ordinal))
        {
            throw new PettyCashVerifierCannotApproveException(doc.ID);
        }

        var fund = await _fundRepository.GetForUpdateAsync(doc.FUND_ID, vahedCode, cancellationToken)
            ?? throw new NotFoundException("PettyCashFund", doc.FUND_ID);

        var totalAmount = (doc.AMOUNT_BEFORE_TAX ?? 0m) + (doc.VAT_AMOUNT ?? 0m);

        // ChiefExecutive has no amount ceiling; a caller whose only qualifying role is
        // FinanceManager is capped at the fund's own configured limit.
        if (!callerRoles.Contains(PettyCashRole.ChiefExecutive) && totalAmount > fund.FINANCE_MANAGER_APPROVAL_LIMIT)
        {
            throw new PettyCashApprovalAuthorityExceededException(doc.ID, totalAmount, fund.FINANCE_MANAGER_APPROVAL_LIMIT);
        }

        var now = DateTime.UtcNow;

        doc.DOC_STATE = PettyCashDocState.Approved;
        doc.CHANGEUSERID = userId;
        doc.UPDATEDDATE = now;

        var head = await _chargeAndCostRepository.GetHeadForUpdateAsync(doc.CHARGEANDCOSTHEAD_ID, vahedCode, cancellationToken);

        if (head is not null)
        {
            head.STATUS = PettyCashStatusMap.ToLegacyStatus(PettyCashDocState.Approved);
            head.CHANGEUSERID = userId;
            head.UPDATEDDATE = now;
        }

        await _eventRepository.AddAsync(
            new TB_PC_DOC_EVENT
            {
                ID = Guid.NewGuid(),
                EXPENSE_DOC_ID = doc.ID,
                ACTION = PettyCashDocAction.FinalApprove,
                FROM_STATE = fromState,
                TO_STATE = PettyCashDocState.Approved,
                NOTE = note,
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
