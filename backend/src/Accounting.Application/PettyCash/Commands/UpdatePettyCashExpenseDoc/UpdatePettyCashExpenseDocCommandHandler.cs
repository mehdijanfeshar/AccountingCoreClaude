using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UpdatePettyCashExpenseDoc;

public sealed class UpdatePettyCashExpenseDocCommandHandler : IRequestHandler<UpdatePettyCashExpenseDocCommand>
{
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IRevolvingFundRepository _revolvingFundRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IPettyCashDocEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    private const int LegacyPayToMaxLength = 100;

    public UpdatePettyCashExpenseDocCommandHandler(
        IPettyCashExpenseDocRepository expenseDocRepository,
        IChargeAndCostRepository chargeAndCostRepository,
        IRevolvingFundRepository revolvingFundRepository,
        IExpenseRepository expenseRepository,
        IPettyCashDocEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _expenseDocRepository = expenseDocRepository;
        _chargeAndCostRepository = chargeAndCostRepository;
        _revolvingFundRepository = revolvingFundRepository;
        _expenseRepository = expenseRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task Handle(UpdatePettyCashExpenseDocCommand request, CancellationToken cancellationToken)
    {
        var doc = await _expenseDocRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (doc is null || doc.ISDELETED)
        {
            throw new NotFoundException("PettyCashExpenseDoc", request.Id);
        }

        PettyCashDocEditability.EnsureEditable(doc.ID, doc.DOC_STATE);

        _ = await _revolvingFundRepository.GetForUpdateAsync(request.FundId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("RevolvingFund", request.FundId);

        _ = await _expenseRepository.GetForUpdateAsync(request.ExpenseId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("Expense", request.ExpenseId);

        if (!string.IsNullOrWhiteSpace(request.VendorNationalId) && !string.IsNullOrWhiteSpace(request.InvoiceNo))
        {
            var duplicate = await _expenseDocRepository.ExistsActiveDuplicateAsync(
                request.VendorNationalId, request.InvoiceNo, request.VahedCode, excludeId: doc.ID, cancellationToken);

            if (duplicate)
            {
                throw new PettyCashDuplicateExpenseDocException(request.VendorNationalId, request.InvoiceNo);
            }
        }

        var year = request.Year;
        var totalAmount = request.AmountBeforeTax + request.VatAmount;
        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        doc.REVOLVINGFUND_ID = request.FundId;
        doc.VENDOR_NAME = request.VendorName;
        doc.VENDOR_NATIONAL_ID = request.VendorNationalId;
        doc.INVOICE_NO = request.InvoiceNo;
        doc.INVOICE_DATE = request.InvoiceDate;
        doc.EVIDENCE_TYPE = request.EvidenceType;
        doc.AMOUNT_BEFORE_TAX = request.AmountBeforeTax;
        doc.VAT_AMOUNT = request.VatAmount;
        doc.YEAR = year;
        doc.CHANGEUSERID = userId;
        doc.UPDATEDDATE = now;

        var head = await _chargeAndCostRepository.GetHeadForUpdateAsync(doc.CHARGEANDCOSTHEAD_ID, request.VahedCode, cancellationToken);

        if (head is not null)
        {
            head.CHARGEANDCOST_DATE = request.RegisterDate;
            head.DESCRIPTION = request.Description;
            head.YEAR = year;
            head.CHANGEUSERID = userId;
            head.UPDATEDDATE = now;
        }

        var detail = await _chargeAndCostRepository.GetSingleDetailForUpdateAsync(doc.CHARGEANDCOSTHEAD_ID, cancellationToken);

        if (detail is not null)
        {
            var payTo = request.VendorName.Length > LegacyPayToMaxLength
                ? request.VendorName[..LegacyPayToMaxLength]
                : request.VendorName;

            detail.EXPENSE_ID = request.ExpenseId;
            // Always stays null — see CreatePettyCashExpenseDocCommandHandler's comment on the
            // same field.
            detail.REVOLVINGFUND_ID = null;
            detail.PAYTO = payTo;
            detail.CREDITOR = totalAmount;
            detail.YEAR = year;
            detail.CHANGEUSERID = userId;
            detail.UPDATEDDATE = now;
        }

        await _eventRepository.AddAsync(
            new TB_PC_DOC_EVENT
            {
                ID = Guid.NewGuid(),
                EXPENSE_DOC_ID = doc.ID,
                ACTION = PettyCashDocAction.Update,
                FROM_STATE = doc.DOC_STATE,
                TO_STATE = doc.DOC_STATE,
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
