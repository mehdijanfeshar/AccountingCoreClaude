using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.VerifyPettyCashExpenseDoc;

public sealed class VerifyPettyCashExpenseDocCommandHandler : IRequestHandler<VerifyPettyCashExpenseDocCommand>
{
    private static readonly PettyCashRole[] AllowedRoles = { PettyCashRole.Inspector };

    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IPettyCashDocEventRepository _eventRepository;
    private readonly IPettyCashReviewAuthorizer _reviewAuthorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public VerifyPettyCashExpenseDocCommandHandler(
        IPettyCashExpenseDocRepository expenseDocRepository,
        IPettyCashDocEventRepository eventRepository,
        IPettyCashReviewAuthorizer reviewAuthorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _expenseDocRepository = expenseDocRepository;
        _eventRepository = eventRepository;
        _reviewAuthorizer = reviewAuthorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task Handle(VerifyPettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        var doc = await _expenseDocRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (doc is null || doc.ISDELETED)
        {
            throw new NotFoundException("PettyCashExpenseDoc", request.Id);
        }

        // Reviewer-of-this-fund-as-Inspector check → 403, then creator SoD → 409 — same order as
        // every other review action, before the state/already-verified checks below.
        await _reviewAuthorizer.EnsureCanReviewAsync(doc.ID, doc.FUND_ID, doc.ADDUSERID, AllowedRoles, cancellationToken);

        if (doc.DOC_STATE != PettyCashDocState.PendingReview)
        {
            throw new PettyCashReviewStateConflictException(
                doc.ID, doc.DOC_STATE, PettyCashDocState.PendingReview, PettyCashDocAction.Verify);
        }

        if (doc.VERIFIED_BY_USERID is not null)
        {
            throw new PettyCashAlreadyVerifiedException(doc.ID);
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        doc.VERIFIED_BY_USERID = userId;
        doc.VERIFIED_DATE = now;
        doc.CHANGEUSERID = userId;
        doc.UPDATEDDATE = now;

        // DOC_STATE (and therefore the Legacy head's STATUS via PettyCashStatusMap) does not
        // change — Verify only records that control has been checked.
        await _eventRepository.AddAsync(
            new TB_PC_DOC_EVENT
            {
                ID = Guid.NewGuid(),
                EXPENSE_DOC_ID = doc.ID,
                ACTION = PettyCashDocAction.Verify,
                FROM_STATE = PettyCashDocState.PendingReview,
                TO_STATE = PettyCashDocState.PendingReview,
                NOTE = request.Note,
                CLIENT_IP = _clientInfoProvider.ClientIp,
                VAHEDCODE = request.VahedCode,
                YEAR = doc.YEAR,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
