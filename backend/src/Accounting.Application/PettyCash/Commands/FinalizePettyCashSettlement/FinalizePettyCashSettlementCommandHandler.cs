using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.FinalizePettyCashSettlement;

/// <summary>
/// The single write path that turns a تنخواه settlement period into a posted GL voucher — بخش
/// ۳-ب (<c>docs/tankhah-khazaneh-module.md</c> section 9). This is the most sensitive handler this
/// batch adds (it writes <see cref="TB_VOUCHERSHEAD"/>/<see cref="TB_VOUCHERSDETAIL"/>), so every
/// validation below runs BEFORE <see cref="IPettyCashSettlementVoucherBuilder.BuildAndStageAsync"/>
/// is called, and the voucher + period + every affected صورت‌هزینه + one <see cref="TB_PC_DOC_EVENT"/>
/// per document are all staged together and persisted by exactly ONE
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call, wrapped in an explicit
/// <see cref="IUnitOfWork.BeginTransactionAsync"/>/<see cref="IUnitOfWork.CommitTransactionAsync"/>
/// pair (rolled back on any failure) — per CLAUDE.md's rule that سند/تولید شماره/بستن دوره use
/// cases need an explicit transaction boundary, not just a bare SaveChanges.
///
/// <b>Concurrency:</b> optimistic-by-construction, not lock-based — <c>TB_PC_SETTLEMENT_PERIOD</c>
/// has no version column, but a period can only ever be finalized once (its <c>STATE</c> flips to
/// <see cref="PettyCashSettlementState.Final"/> in the same SaveChanges call that creates the
/// voucher), and <c>IPettyCashSettlementPeriodProvisioner.EnsureDraftAsync</c> never returns an
/// already-Final row. A genuine race between two concurrent finalize calls for the same fund
/// would both load the same Draft row and both attempt to write it; the second SaveChanges either
/// succeeds harmlessly-redundantly (same numbers) or surfaces as a generic 500 — no idempotency
/// key was added for this narrow window, matching the documented gap already accepted on
/// <c>CreateVoucherDetailCommandHandler</c> for the analogous head-soft-delete race.
///
/// TB_CHARGEANDCOST_HEAD.STATUS is deliberately left untouched here:
/// <see cref="Accounting.Application.Common.Security.PettyCashStatusMap.ToLegacyStatus(PettyCashDocState)"/>
/// already maps both <see cref="PettyCashDocState.Approved"/> and <see cref="PettyCashDocState.Settled"/>
/// to <see cref="ChargeAndCostStatus.Accepted"/> — the Legacy column's value does not change when
/// a document is settled, so there is nothing to write there.
/// </summary>
public sealed class FinalizePettyCashSettlementCommandHandler
    : IRequestHandler<FinalizePettyCashSettlementCommand, FinalizePettyCashSettlementResult>
{
    private readonly IPettyCashFundRepository _fundRepository;
    private readonly IPettyCashFundReviewerRepository _reviewerRepository;
    private readonly IPettyCashSettlementPeriodProvisioner _provisioner;
    private readonly IPettyCashSettlementReadRepository _settlementReadRepository;
    private readonly IPettyCashExpenseDocRepository _expenseDocRepository;
    private readonly IPettyCashDocEventRepository _eventRepository;
    private readonly IPettyCashSettlementVoucherBuilder _voucherBuilder;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClientInfoProvider _clientInfoProvider;

    public FinalizePettyCashSettlementCommandHandler(
        IPettyCashFundRepository fundRepository,
        IPettyCashFundReviewerRepository reviewerRepository,
        IPettyCashSettlementPeriodProvisioner provisioner,
        IPettyCashSettlementReadRepository settlementReadRepository,
        IPettyCashExpenseDocRepository expenseDocRepository,
        IPettyCashDocEventRepository eventRepository,
        IPettyCashSettlementVoucherBuilder voucherBuilder,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClientInfoProvider clientInfoProvider)
    {
        _fundRepository = fundRepository;
        _reviewerRepository = reviewerRepository;
        _provisioner = provisioner;
        _settlementReadRepository = settlementReadRepository;
        _expenseDocRepository = expenseDocRepository;
        _eventRepository = eventRepository;
        _voucherBuilder = voucherBuilder;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task<FinalizePettyCashSettlementResult> Handle(
        FinalizePettyCashSettlementCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepository.GetForUpdateAsync(request.FundId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("PettyCashFund", request.FundId);

        // §۹ — "فقط SeniorAccountant همان تنخواه".
        var activeRoles = await _reviewerRepository.GetActiveRolesAsync(request.FundId, _currentUser.UserId, cancellationToken);

        if (!activeRoles.Contains(PettyCashRole.SeniorAccountant))
        {
            throw new PettyCashSettlementRoleRequiredException(request.FundId);
        }

        var period = await _provisioner.EnsureDraftAsync(fund, request.VahedCode, cancellationToken);

        var movement = await _settlementReadRepository.GetPeriodMovementAsync(
            fund.ID, request.VahedCode, period.PERIOD_START, period.PERIOD_END, cancellationToken);

        if (movement.DocIds.Count == 0)
        {
            throw new PettyCashSettlementNoDocumentsException(fund.ID);
        }

        // SoD — loaded (tracked) here so the same entities are mutated to Settled below, once
        // every other validation has passed.
        var docs = await _expenseDocRepository.GetManyForUpdateAsync(movement.DocIds, request.VahedCode, cancellationToken);

        if (docs.Any(d => string.Equals(d.ADDUSERID, _currentUser.UserId, StringComparison.Ordinal)))
        {
            throw new PettyCashSettlementSoDConflictException(fund.ID);
        }

        var exposure = await _expenseDocRepository.GetFundExposureAsync(
            fund.ID, request.VahedCode, excludeDocId: null, cancellationToken);

        if (exposure.InFlightCount > 0 && !request.AcknowledgeInFlightTransfer)
        {
            throw new PettyCashSettlementInFlightAcknowledgeRequiredException(fund.ID, exposure.InFlightCount, exposure.InFlightAmount);
        }

        if (period.COUNTED_BALANCE is not { } countedBalance)
        {
            throw new PettyCashSettlementCountedBalanceRequiredException(fund.ID, period.ID);
        }

        var closingCashBalance = period.OPENING_BALANCE + movement.ReplenishmentsAndRefunds - movement.ApprovedExpensesTotal;

        if (countedBalance != closingCashBalance)
        {
            throw new PettyCashSettlementCountedBalanceMismatchException(fund.ID, countedBalance, closingCashBalance);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var voucherResult = await _voucherBuilder.BuildAndStageAsync(
                fund, period, movement.ExpenseGroups, request.VahedCode, cancellationToken);

            var now = DateTime.UtcNow;

            foreach (var doc in docs)
            {
                doc.DOC_STATE = PettyCashDocState.Settled;
                doc.CHANGEUSERID = _currentUser.UserId;
                doc.UPDATEDDATE = now;

                await _eventRepository.AddAsync(
                    new TB_PC_DOC_EVENT
                    {
                        ID = Guid.NewGuid(),
                        EXPENSE_DOC_ID = doc.ID,
                        ACTION = PettyCashDocAction.Settle,
                        FROM_STATE = PettyCashDocState.Approved,
                        TO_STATE = PettyCashDocState.Settled,
                        NOTE = $"منظور در سند تسویه دوره — شماره سند {voucherResult.DocNum}",
                        CLIENT_IP = _clientInfoProvider.ClientIp,
                        VAHEDCODE = request.VahedCode,
                        YEAR = doc.YEAR,
                        ADDUSERID = _currentUser.UserId,
                        CREATEDDATE = now,
                        ISDELETED = false,
                    },
                    cancellationToken);
            }

            period.STATE = PettyCashSettlementState.Final;
            period.VOUCHERSHEAD_ID = voucherResult.VoucherHeadId;
            period.FINALIZED_BY_USERID = _currentUser.UserId;
            period.FINALIZED_DATE = now;
            period.CHANGEUSERID = _currentUser.UserId;
            period.UPDATEDDATE = now;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new FinalizePettyCashSettlementResult(period.ID, voucherResult.VoucherHeadId, voucherResult.DocNum, docs.Count);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
