using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashExpenseDoc;

/// <summary>
/// Builds the composite صورت‌هزینه aggregate — <see cref="TB_CHARGEANDCOST_HEAD"/> (type
/// هزینه‌کرد) + exactly one <see cref="TB_CHARGEANDCOST_DETAIL"/> + <see cref="TB_PC_EXPENSE_DOC"/>
/// + one <see cref="TB_PC_DOC_EVENT"/> — and persists all four with a single
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call, per
/// <c>docs/tankhah-khazaneh-module.md</c> §4/§5. See <see cref="CreatePettyCashExpenseDocCommand"/>
/// for the fiscal-year derivation assumption.
/// </summary>
public sealed class CreatePettyCashExpenseDocCommandHandler : IRequestHandler<CreatePettyCashExpenseDocCommand, Guid>
{
    private readonly IRevolvingFundRepository _revolvingFundRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IPettyCashDocEventRepository _eventRepository;
    private readonly IPettyCashSubmitRuleChecker _submitRuleChecker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    /// <summary>PAYTO on TB_CHARGEANDCOST_DETAIL is VARCHAR2(100) in Legacy, while VENDOR_NAME on
    /// the new side table is VARCHAR2(200) — see design §5, "PAYTO=vendorName truncated to
    /// 100".</summary>
    private const int LegacyPayToMaxLength = 100;

    public CreatePettyCashExpenseDocCommandHandler(
        IRevolvingFundRepository revolvingFundRepository,
        IExpenseRepository expenseRepository,
        IChargeAndCostRepository chargeAndCostRepository,
        IPettyCashExpenseDocRepository expenseDocRepository,
        IPettyCashDocEventRepository eventRepository,
        IPettyCashSubmitRuleChecker submitRuleChecker,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _revolvingFundRepository = revolvingFundRepository;
        _expenseRepository = expenseRepository;
        _chargeAndCostRepository = chargeAndCostRepository;
        _expenseDocRepository = expenseDocRepository;
        _eventRepository = eventRepository;
        _submitRuleChecker = submitRuleChecker;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task<Guid> Handle(CreatePettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        var fund = await _revolvingFundRepository.GetForUpdateAsync(request.FundId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("RevolvingFund", request.FundId);

        _ = await _expenseRepository.GetForUpdateAsync(request.ExpenseId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("Expense", request.ExpenseId);

        var year = request.Year;

        if (!string.IsNullOrWhiteSpace(request.VendorNationalId) && !string.IsNullOrWhiteSpace(request.InvoiceNo))
        {
            var duplicate = await _expenseDocRepository.ExistsActiveDuplicateAsync(
                request.VendorNationalId, request.InvoiceNo, request.VahedCode, excludeId: null, cancellationToken);

            if (duplicate)
            {
                throw new PettyCashDuplicateExpenseDocException(request.VendorNationalId, request.InvoiceNo);
            }
        }

        var docId = Guid.NewGuid();
        var totalAmount = request.AmountBeforeTax + request.VatAmount;
        var initialState = request.Submit ? PettyCashDocState.New : PettyCashDocState.Draft;

        if (request.Submit)
        {
            await _submitRuleChecker.EnsureSubmittableAsync(
                docId,
                request.FundId,
                fund.DEFAULTAMOUNT,
                request.InvoiceDate,
                year,
                totalAmount,
                request.VahedCode,
                excludeDocId: null,
                cancellationToken);
        }

        var nextCode = await _chargeAndCostRepository.GetNextCodeAsync(
            request.VahedCode, year, ChargeAndCostType.Cost, cancellationToken);
        var code = nextCode.ToString("00000");

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        var head = new TB_CHARGEANDCOST_HEAD
        {
            ID = Guid.NewGuid(),
            CHARGEANDCOST_TYPE = ChargeAndCostType.Cost,
            CHARGEANDCOST_CODE = code,
            CHARGEANDCOST_DATE = request.RegisterDate,
            DESCRIPTION = request.Description,
            STATUS = PettyCashStatusMap.ToLegacyStatus(initialState),
            VAHEDCODE = request.VahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _chargeAndCostRepository.AddHeadAsync(head, cancellationToken);

        var payTo = request.VendorName.Length > LegacyPayToMaxLength
            ? request.VendorName[..LegacyPayToMaxLength]
            : request.VendorName;

        var detail = new TB_CHARGEANDCOST_DETAIL
        {
            ID = Guid.NewGuid(),
            CHARGEANDCOSTHEAD_ID = head.ID,
            EXPENSE_ID = request.ExpenseId,
            // Always null — existing live TB_CHARGEANDCOST_DETAIL rows never carry a fund either;
            // TB_PC_EXPENSE_DOC.REVOLVINGFUND_ID is the real source of truth. See design doc §1.
            REVOLVINGFUND_ID = null,
            PAYTO = payTo,
            DEBTOR = 0,
            CREDITOR = totalAmount,
            PAIDAMOUNT = 0,
            VAHEDCODE = request.VahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _chargeAndCostRepository.AddDetailAsync(detail, cancellationToken);

        var doc = new TB_PC_EXPENSE_DOC
        {
            ID = docId,
            CHARGEANDCOSTHEAD_ID = head.ID,
            REVOLVINGFUND_ID = request.FundId,
            DOC_STATE = initialState,
            VENDOR_NAME = request.VendorName,
            VENDOR_NATIONAL_ID = request.VendorNationalId,
            INVOICE_NO = request.InvoiceNo,
            INVOICE_DATE = request.InvoiceDate,
            EVIDENCE_TYPE = request.EvidenceType,
            AMOUNT_BEFORE_TAX = request.AmountBeforeTax,
            VAT_AMOUNT = request.VatAmount,
            SUBMITTED_DATE = request.Submit ? now : null,
            VAHEDCODE = request.VahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _expenseDocRepository.AddAsync(doc, cancellationToken);

        await _eventRepository.AddAsync(
            new TB_PC_DOC_EVENT
            {
                ID = Guid.NewGuid(),
                EXPENSE_DOC_ID = docId,
                ACTION = PettyCashDocAction.Create,
                FROM_STATE = null,
                TO_STATE = initialState,
                CLIENT_IP = _clientInfoProvider.ClientIp,
                VAHEDCODE = request.VahedCode,
                YEAR = year,
                ADDUSERID = userId,
                CREATEDDATE = now,
                ISDELETED = false,
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return docId;
    }
}
