using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashExpenseDoc;

public sealed class DeletePettyCashExpenseDocCommandHandler : IRequestHandler<DeletePettyCashExpenseDocCommand>
{
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IPettyCashDocEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public DeletePettyCashExpenseDocCommandHandler(
        IPettyCashExpenseDocRepository expenseDocRepository,
        IChargeAndCostRepository chargeAndCostRepository,
        IPettyCashDocEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _expenseDocRepository = expenseDocRepository;
        _chargeAndCostRepository = chargeAndCostRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task Handle(DeletePettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        var doc = await _expenseDocRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (doc is null)
        {
            throw new NotFoundException("PettyCashExpenseDoc", request.Id);
        }

        // Checked BEFORE the idempotent already-deleted short-circuit, so a document that is
        // locked in a non-Draft state reports why rather than silently succeeding — same
        // ordering as DeleteVoucherHeadCommandHandler.
        PettyCashDocEditability.EnsureDeletable(doc.ID, doc.DOC_STATE);

        if (doc.ISDELETED)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        doc.ISDELETED = true;
        doc.CHANGEUSERID = userId;
        doc.UPDATEDDATE = now;

        var head = await _chargeAndCostRepository.GetHeadForUpdateAsync(doc.CHARGEANDCOSTHEAD_ID, request.VahedCode, cancellationToken);

        if (head is not null && !head.ISDELETED)
        {
            head.ISDELETED = true;
            head.CHANGEUSERID = userId;
            head.UPDATEDDATE = now;
        }

        var detail = await _chargeAndCostRepository.GetSingleDetailForUpdateAsync(doc.CHARGEANDCOSTHEAD_ID, cancellationToken);

        if (detail is not null && !detail.ISDELETED)
        {
            detail.ISDELETED = true;
            detail.CHANGEUSERID = userId;
            detail.UPDATEDDATE = now;
        }

        await _eventRepository.AddAsync(
            new TB_PC_DOC_EVENT
            {
                ID = Guid.NewGuid(),
                EXPENSE_DOC_ID = doc.ID,
                ACTION = PettyCashDocAction.Delete,
                FROM_STATE = doc.DOC_STATE,
                TO_STATE = null,
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
