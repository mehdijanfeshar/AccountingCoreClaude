using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashReplenishment;

/// <summary>
/// Builds the composite ترمیم aggregate — see <see cref="CreatePettyCashReplenishmentCommand"/>
/// XML doc for the full shape — and persists everything with a single
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call.
/// </summary>
public sealed class CreatePettyCashReplenishmentCommandHandler : IRequestHandler<CreatePettyCashReplenishmentCommand, Guid>
{
    private readonly IPettyCashFundRepository _fundRepository;
    private readonly IBankAccountReadRepository _bankAccountReadRepository;
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IChargeAndCostRepository _chargeAndCostRepository;
    private readonly IPettyCashReplenishmentRepository _replenishmentRepository;
    private readonly IPettyCashReplenishmentAuthorizer _authorizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreatePettyCashReplenishmentCommandHandler(
        IPettyCashFundRepository fundRepository,
        IBankAccountReadRepository bankAccountReadRepository,
        IPettyCashExpenseDocRepository expenseDocRepository,
        IChargeAndCostRepository chargeAndCostRepository,
        IPettyCashReplenishmentRepository replenishmentRepository,
        IPettyCashReplenishmentAuthorizer authorizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _fundRepository = fundRepository;
        _bankAccountReadRepository = bankAccountReadRepository;
        _expenseDocRepository = expenseDocRepository;
        _chargeAndCostRepository = chargeAndCostRepository;
        _replenishmentRepository = replenishmentRepository;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreatePettyCashReplenishmentCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepository.GetForUpdateAsync(request.FundId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("PettyCashFund", request.FundId);

        // تصمیم محافظه‌کارانه (۲۰۲۶-۰۹-۲۸): ایجاد/ارسال/حذف پیش‌نویس ترمیم فقط FinanceManager یا
        // Treasurer همان تنخواه — رجوع به CreatePettyCashReplenishmentCommand XML doc.
        await _authorizer.EnsureHasRoleAsync(
            request.FundId, new[] { PettyCashRole.FinanceManager, PettyCashRole.Treasurer }, cancellationToken);

        var sourceAccount = await _bankAccountReadRepository.GetByIdAsync(request.SourceBankAccountId, request.VahedCode, cancellationToken);

        if (sourceAccount is null)
        {
            throw new NotFoundException("BankAccount", request.SourceBankAccountId);
        }

        var candidates = await _expenseDocRepository.GetApprovedUnlinkedByFundAsync(
            request.FundId, request.VahedCode, cancellationToken);

        if (candidates.Count == 0)
        {
            throw new PettyCashNoDocumentsToReplenishException(request.FundId);
        }

        var totalAmount = candidates.Sum(c => c.Amount);

        // Defensive belt-and-suspenders check — see PettyCashReplenishmentExceedsCeilingException
        // XML doc for why this can never actually fire in practice.
        if (totalAmount > fund.CEILING)
        {
            throw new PettyCashReplenishmentExceedsCeilingException(request.FundId, totalAmount, fund.CEILING);
        }

        var year = request.Year;
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;
        var initialState = request.Submit ? PettyCashReplenishmentState.PendingFinanceManager : PettyCashReplenishmentState.Draft;

        var nextCode = await _chargeAndCostRepository.GetNextCodeAsync(
            request.VahedCode, year, ChargeAndCostType.Charge, cancellationToken);

        var head = new TB_CHARGEANDCOST_HEAD
        {
            ID = Guid.NewGuid(),
            CHARGEANDCOST_TYPE = ChargeAndCostType.Charge,
            CHARGEANDCOST_CODE = nextCode.ToString("00000"),
            CHARGEANDCOST_DATE = request.RegisterDate,
            DESCRIPTION = request.Note,
            STATUS = PettyCashStatusMap.ToLegacyStatus(initialState),
            // حساب بانکی مبدأ — design §۳-الف: "ACCOUNT_ID آن = حساب بانکی مبدأ".
            ACCOUNT_ID = request.SourceBankAccountId,
            VAHEDCODE = request.VahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _chargeAndCostRepository.AddHeadAsync(head, cancellationToken);

        var replenishment = new TB_PC_REPLENISHMENT
        {
            ID = Guid.NewGuid(),
            CHARGEANDCOSTHEAD_ID = head.ID,
            FUND_ID = request.FundId,
            CODE = "RCH-" + nextCode.ToString("00000"),
            PAYMENT_METHOD = request.PaymentMethod,
            STATE = initialState,
            TOTAL_AMOUNT = totalAmount,
            NOTE = request.Note,
            VAHEDCODE = request.VahedCode,
            YEAR = year,
            ADDUSERID = userId,
            CREATEDDATE = now,
            ISDELETED = false,
        };

        await _replenishmentRepository.AddAsync(replenishment, cancellationToken);

        foreach (var candidate in candidates)
        {
            // Re-checked here (not just trusted from the snapshot read above) so a race with a
            // concurrent ترمیم request over the same صورت‌هزینه is caught before the link is
            // staged — see PettyCashDocAlreadyReplenishedException XML doc.
            var alreadyLinked = await _chargeAndCostRepository.ExistsActiveLinkForCostAsync(candidate.CostId, cancellationToken);

            if (alreadyLinked)
            {
                throw new PettyCashDocAlreadyReplenishedException(candidate.ExpenseDocId);
            }

            await _chargeAndCostRepository.AddLinkAsync(
                new TB_CHARGE_LINK_COST
                {
                    ID = Guid.NewGuid(),
                    CHARGE_ID = head.ID,
                    COST_ID = candidate.CostId,
                    AMOUNT = candidate.Amount,
                    VAHEDCODE = request.VahedCode,
                    YEAR = year,
                    ADDUSERID = userId,
                    CREATEDDATE = now,
                    ISDELETED = false,
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return replenishment.ID;
    }
}
