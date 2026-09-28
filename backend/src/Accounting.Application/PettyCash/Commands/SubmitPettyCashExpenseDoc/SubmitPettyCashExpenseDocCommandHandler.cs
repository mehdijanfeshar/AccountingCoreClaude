using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.SubmitPettyCashExpenseDoc;

public sealed class SubmitPettyCashExpenseDocCommandHandler : IRequestHandler<SubmitPettyCashExpenseDocCommand>
{
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IPettyCashFundRepository _pettyCashFundRepository;
    private readonly IPettyCashDocEventRepository _eventRepository;
    private readonly IPettyCashSubmitRuleChecker _submitRuleChecker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public SubmitPettyCashExpenseDocCommandHandler(
        IPettyCashExpenseDocRepository expenseDocRepository,
        IChargeAndCostRepository chargeAndCostRepository,
        IPettyCashFundRepository pettyCashFundRepository,
        IPettyCashDocEventRepository eventRepository,
        IPettyCashSubmitRuleChecker submitRuleChecker,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _expenseDocRepository = expenseDocRepository;
        _chargeAndCostRepository = chargeAndCostRepository;
        _pettyCashFundRepository = pettyCashFundRepository;
        _eventRepository = eventRepository;
        _submitRuleChecker = submitRuleChecker;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task Handle(SubmitPettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        var doc = await _expenseDocRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (doc is null || doc.ISDELETED)
        {
            throw new NotFoundException("PettyCashExpenseDoc", request.Id);
        }

        var fromState = doc.DOC_STATE;

        PettyCashDocEditability.EnsureEditable(doc.ID, fromState);

        var fund = await _pettyCashFundRepository.GetForUpdateAsync(doc.FUND_ID, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("PettyCashFund", doc.FUND_ID);

        // 2026-09-28 rule: an inactive تنخواه accepts no Submit either.
        if (!fund.IS_ACTIVE)
        {
            throw new PettyCashFundInactiveException(doc.FUND_ID);
        }

        var totalAmount = (doc.AMOUNT_BEFORE_TAX ?? 0m) + (doc.VAT_AMOUNT ?? 0m);
        var year = doc.YEAR ?? string.Empty;

        // Excludes the document's own current row from the exposure sum — correct whether it was
        // Draft (not counted yet) or Returned (already counted as in-flight); see
        // IPettyCashSubmitRuleChecker XML doc.
        await _submitRuleChecker.EnsureSubmittableAsync(
            doc.ID,
            doc.FUND_ID,
            fund.CEILING,
            fund.PER_DOC_LIMIT,
            doc.INVOICE_DATE,
            year,
            totalAmount,
            request.VahedCode,
            excludeDocId: doc.ID,
            cancellationToken);

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        doc.DOC_STATE = PettyCashDocState.New;
        doc.SUBMITTED_DATE = now;
        doc.CHANGEUSERID = userId;
        doc.UPDATEDDATE = now;

        var head = await _chargeAndCostRepository.GetHeadForUpdateAsync(doc.CHARGEANDCOSTHEAD_ID, request.VahedCode, cancellationToken);

        if (head is not null)
        {
            head.STATUS = PettyCashStatusMap.ToLegacyStatus(PettyCashDocState.New);
            head.CHANGEUSERID = userId;
            head.UPDATEDDATE = now;
        }

        await _eventRepository.AddAsync(
            new TB_PC_DOC_EVENT
            {
                ID = Guid.NewGuid(),
                EXPENSE_DOC_ID = doc.ID,
                ACTION = PettyCashDocAction.Submit,
                FROM_STATE = fromState,
                TO_STATE = PettyCashDocState.New,
                CLIENT_IP = _clientInfoProvider.ClientIp,
                VAHEDCODE = request.VahedCode,
                YEAR = year,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
