using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPettyCashSettlementReadRepository"/>. Every
/// source (ترمیم/استرداد/صورت‌هزینه) is fetched as its own plain, flat query and merged/grouped
/// client-side — same discipline <c>PettyCashLedgerReadRepository</c> documents (never a
/// projection built from a LeftJoin chain, per the module's own §۳-الف Oracle warning). Always
/// <c>AsNoTracking()</c>.
/// </summary>
public sealed class PettyCashSettlementReadRepository : IPettyCashSettlementReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashSettlementReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PettyCashSettlementMovementDto> GetPeriodMovementAsync(
        Guid fundId,
        string vahedCode,
        string periodStart,
        string periodEnd,
        CancellationToken cancellationToken = default)
    {
        var replenishmentTotal = await (
            from r in _dbContext.TB_PC_REPLENISHMENTs.AsNoTracking()
            where r.FUND_ID == fundId && r.VAHEDCODE == vahedCode && !r.ISDELETED
                  && r.STATE == PettyCashReplenishmentState.Paid
            join head in _dbContext.TB_CHARGEANDCOST_HEADs.AsNoTracking() on r.CHARGEANDCOSTHEAD_ID equals head.ID
            where string.Compare(head.CHARGEANDCOST_DATE, periodStart) >= 0
                  && string.Compare(head.CHARGEANDCOST_DATE, periodEnd) <= 0
            select r.TOTAL_AMOUNT)
            .ToListAsync(cancellationToken);

        var refundTotal = await _dbContext.TB_PC_REFUNDs.AsNoTracking()
            .Where(r => r.FUND_ID == fundId && r.VAHEDCODE == vahedCode && !r.ISDELETED
                        && string.Compare(r.REFUND_DATE, periodStart) >= 0
                        && string.Compare(r.REFUND_DATE, periodEnd) <= 0)
            .Select(r => r.AMOUNT)
            .ToListAsync(cancellationToken);

        // §۹ — «تاریخ ثبت ≤ PERIOD_END», عمداً بدون کف پایینی (سند تأییدشدهٔ دیرهنگام هم منظور
        // می‌شود؛ رجوع به IPettyCashSettlementReadRepository.GetPeriodMovementAsync XML doc).
        var expenseRows = await (
            from doc in _dbContext.TB_PC_EXPENSE_DOCs.AsNoTracking()
            where doc.FUND_ID == fundId && doc.VAHEDCODE == vahedCode && !doc.ISDELETED
                  && doc.DOC_STATE == PettyCashDocState.Approved
            join head in _dbContext.TB_CHARGEANDCOST_HEADs.AsNoTracking() on doc.CHARGEANDCOSTHEAD_ID equals head.ID
            where string.Compare(head.CHARGEANDCOST_DATE, periodEnd) <= 0
            join detail in _dbContext.TB_CHARGEANDCOST_DETAILs.AsNoTracking()
                on (Guid?)doc.CHARGEANDCOSTHEAD_ID equals detail.CHARGEANDCOSTHEAD_ID
            join expense in _dbContext.TB_EXPENCEs.AsNoTracking()
                on detail.EXPENSE_ID equals (Guid?)expense.ID into expenseGroup
            from expense in expenseGroup.DefaultIfEmpty()
            select new
            {
                doc.ID,
                doc.AMOUNT_BEFORE_TAX,
                doc.VAT_AMOUNT,
                ExpenseId = detail.EXPENSE_ID,
                AccountCodeId = (Guid?)expense.ACCOUNTCODE_ID,
            })
            .ToListAsync(cancellationToken);

        var expenseIds = expenseRows.Where(r => r.ExpenseId.HasValue).Select(r => r.ExpenseId!.Value).Distinct().ToList();

        var accountCodeIds = expenseRows.Where(r => r.AccountCodeId.HasValue).Select(r => r.AccountCodeId!.Value).Distinct().ToList();

        var accountCodes = accountCodeIds.Count == 0
            ? new Dictionary<Guid, (string? Code, string? Title)>()
            : (await _dbContext.TB_ACCOUNTCODEs.AsNoTracking()
                .Where(a => accountCodeIds.Contains(a.ID))
                .Select(a => new { a.ID, a.ACCCODE, a.ACCCODENAME })
                .ToListAsync(cancellationToken))
                .ToDictionary(a => a.ID, a => (a.ACCCODE, a.ACCCODENAME));

        var tafsilisByExpense = expenseIds.Count == 0
            ? new Dictionary<Guid, List<PettyCashSettlementTafsiliDto>>()
            : await LoadExpenseTafsilisAsync(expenseIds, cancellationToken);

        var groups = expenseRows
            .GroupBy(r => r.ExpenseId)
            .Where(g => g.Key.HasValue)
            .Select(g =>
            {
                var expenseId = g.Key!.Value;
                var accountCodeId = g.First().AccountCodeId;
                var (code, title) = accountCodeId.HasValue && accountCodes.TryGetValue(accountCodeId.Value, out var info)
                    ? info
                    : (null, null);
                var tafsilis = tafsilisByExpense.TryGetValue(expenseId, out var links)
                    ? (IReadOnlyList<PettyCashSettlementTafsiliDto>)links
                    : Array.Empty<PettyCashSettlementTafsiliDto>();

                return new PettyCashSettlementExpenseGroupDto(
                    expenseId,
                    accountCodeId,
                    code,
                    title,
                    g.Sum(x => (x.AMOUNT_BEFORE_TAX ?? 0m) + (x.VAT_AMOUNT ?? 0m)),
                    g.Count(),
                    g.Select(x => x.ID).ToList(),
                    tafsilis);
            })
            .OrderBy(l => l.AccountCode)
            .ToList();

        // DocIds is derived from the GROUPED set (not the raw expenseRows) so a document whose
        // TB_CHARGEANDCOST_DETAIL.EXPENSE_ID is somehow null (never happens through the normal
        // create path, which requires it — defensive only) is never marked Settled without a
        // voucher line backing it: the two must always stay in lock-step.
        return new PettyCashSettlementMovementDto(
            replenishmentTotal.Sum() + refundTotal.Sum(),
            groups,
            groups.Sum(g => g.Amount),
            groups.SelectMany(g => g.DocIds).ToList());
    }

    private async Task<Dictionary<Guid, List<PettyCashSettlementTafsiliDto>>> LoadExpenseTafsilisAsync(
        IReadOnlyList<Guid> expenseIds, CancellationToken cancellationToken)
    {
        var rows = await (
            from link in _dbContext.TB_EXPENCE_LINK_TAFSILIs.AsNoTracking()
            where expenseIds.Contains(link.EXPENSE_ID) && !link.ISDELETED
            join tafsili in _dbContext.TB_TAFSILIs.AsNoTracking() on link.TAFSILI_ID equals tafsili.ID
            join level in _dbContext.TB_LEVEL_TAFSILs.AsNoTracking() on link.LEVEL_ID equals level.ID
            select new
            {
                link.EXPENSE_ID,
                link.TAFSILI_ID,
                link.LEVEL_ID,
                tafsili.TAFSILI_CODE,
                tafsili.TAFSILI_NAME,
                level.LEVEL_NAME,
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.EXPENSE_ID)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new PettyCashSettlementTafsiliDto(
                    r.TAFSILI_ID, r.LEVEL_ID, r.TAFSILI_CODE, r.TAFSILI_NAME, r.LEVEL_NAME)).ToList());
    }

    public async Task<IReadOnlyList<PettyCashSettlementTafsiliDto>> GetFundTafsilisAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var rows = await (
            from link in _dbContext.TB_PC_FUND_LINK_TAFSILIs.AsNoTracking()
            where link.FUND_ID == fundId && link.VAHEDCODE == vahedCode && !link.ISDELETED
            join tafsili in _dbContext.TB_TAFSILIs.AsNoTracking() on link.TAFSILI_ID equals tafsili.ID
            join level in _dbContext.TB_LEVEL_TAFSILs.AsNoTracking() on link.LEVEL_ID equals level.ID
            select new PettyCashSettlementTafsiliDto(
                link.TAFSILI_ID, link.LEVEL_ID, tafsili.TAFSILI_CODE, tafsili.TAFSILI_NAME, level.LEVEL_NAME))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<PettyCashSettlementHistoryItemDto>> GetFinalHistoryAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var rows = await (
            from period in _dbContext.TB_PC_SETTLEMENT_PERIODs.AsNoTracking()
            where period.FUND_ID == fundId && period.VAHEDCODE == vahedCode && !period.ISDELETED
                  && period.STATE == PettyCashSettlementState.Final
            join voucher in _dbContext.TB_VOUCHERSHEADs.AsNoTracking()
                on period.VOUCHERSHEAD_ID equals (Guid?)voucher.ID into voucherGroup
            from voucher in voucherGroup.DefaultIfEmpty()
            orderby period.PERIOD_END descending
            select new PettyCashSettlementHistoryItemDto(
                period.ID,
                period.PERIOD_START,
                period.PERIOD_END,
                period.OPENING_BALANCE,
                period.COUNTED_BALANCE ?? 0m,
                period.VOUCHERSHEAD_ID,
                voucher!.DOC_NUM,
                period.FINALIZED_BY_USERID,
                period.FINALIZED_DATE))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
