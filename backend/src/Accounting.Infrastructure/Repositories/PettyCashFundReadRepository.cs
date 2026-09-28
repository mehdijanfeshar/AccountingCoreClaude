using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPettyCashFundReadRepository"/>. Reads directly
/// from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> — the same deliberate, narrow
/// exception to the "reports read from a Read Model" rule as <c>RevolvingFundReadRepository</c>
/// (a simple GetAll/join on a handful of live rows per unit, not analytical reporting).
/// </summary>
public sealed class PettyCashFundReadRepository : IPettyCashFundReadRepository
{
    private static readonly PettyCashDocState[] InFlightStates =
    {
        PettyCashDocState.New,
        PettyCashDocState.PendingReview,
        PettyCashDocState.Returned,
    };

    private sealed record FundRow(
        Guid ID,
        string CODE,
        string NAME,
        string CUSTODIAN_USERID,
        string? CUSTODIAN_NAME,
        decimal CEILING,
        decimal PER_DOC_LIMIT,
        int? ALERT_THRESHOLD_PERCENT,
        Guid? ACCOUNTCODE_ID,
        string? AccountCodeTitle,
        PettyCashSettlementPeriod? SETTLEMENT_PERIOD,
        bool IS_ACTIVE,
        bool ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public PettyCashFundReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PettyCashFundDto>> GetAllAsync(string vahedCode, CancellationToken cancellationToken = default)
    {
        // Same fail-closed, unconditional exact-equality VahedCode filter as every other
        // IVahedScopedQuery repository in this project — see RevolvingFundReadRepository's XML
        // doc for the full rationale.
        var funds = await SelectFundRow(_dbContext.TB_PC_FUNDs.AsNoTracking()
                .Where(f => !f.ISDELETED && f.VAHEDCODE == vahedCode))
            .ToListAsync(cancellationToken);

        if (funds.Count == 0)
        {
            return Array.Empty<PettyCashFundDto>();
        }

        var stats = await GetDocStatsAsync(vahedCode, cancellationToken);

        return funds.Select(fund => ToDto(fund, stats)).ToList();
    }

    public async Task<PettyCashFundDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        // The owning unit is read first as a scalar so the access decision can tell "no such row"
        // (null, caller gets null, 404) from "another unit's row" (403) — same pattern as every
        // other by-id read repository in this project.
        var ownerVahedCode = await _dbContext.TB_PC_FUNDs
            .AsNoTracking()
            .Where(f => f.ID == id)
            .Select(f => f.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "PettyCashFund");

        var fund = await SelectFundRow(_dbContext.TB_PC_FUNDs.AsNoTracking().Where(f => f.ID == id))
            .FirstOrDefaultAsync(cancellationToken);

        if (fund is null)
        {
            return null;
        }

        var stats = await GetDocStatsAsync(vahedCode, cancellationToken, id);

        return ToDto(fund, stats);
    }

    private static IQueryable<FundRow> SelectFundRow(IQueryable<TB_PC_FUND> query) => query.Select(f => new FundRow(
        f.ID,
        f.CODE,
        f.NAME,
        f.CUSTODIAN_USERID,
        f.CUSTODIAN_NAME,
        f.CEILING,
        f.PER_DOC_LIMIT,
        f.ALERT_THRESHOLD_PERCENT,
        f.ACCOUNTCODE_ID,
        f.ACCOUNTCODE!.ACCCODENAME,
        f.SETTLEMENT_PERIOD,
        f.IS_ACTIVE,
        f.ISDELETED));

    /// <summary>
    /// One grouped query for every fund's §2 balance-equation inputs, rather than one query per
    /// fund — the funds list/by-id lookup for a single unit is small, but this keeps it O(1)
    /// queries regardless of how many funds that unit has. <paramref name="onlyFundId"/> narrows to
    /// a single fund for the by-id path, without changing the shape.
    /// </summary>
    private async Task<List<(Guid FundId, PettyCashDocState State, int Count, decimal AmountBeforeTax, decimal Vat)>> GetDocStatsAsync(
        string vahedCode, CancellationToken cancellationToken, Guid? onlyFundId = null)
    {
        var docs = _dbContext.TB_PC_EXPENSE_DOCs
            .AsNoTracking()
            .Where(d => !d.ISDELETED && d.VAHEDCODE == vahedCode);

        if (onlyFundId is { } fundId)
        {
            docs = docs.Where(d => d.FUND_ID == fundId);
        }

        var grouped = await docs
            .GroupBy(d => new { d.FUND_ID, d.DOC_STATE })
            .Select(g => new
            {
                g.Key.FUND_ID,
                g.Key.DOC_STATE,
                Count = g.Count(),
                AmountBeforeTax = g.Sum(x => (decimal?)x.AMOUNT_BEFORE_TAX) ?? 0m,
                Vat = g.Sum(x => (decimal?)x.VAT_AMOUNT) ?? 0m,
            })
            .ToListAsync(cancellationToken);

        return grouped.Select(g => (g.FUND_ID, g.DOC_STATE, g.Count, g.AmountBeforeTax, g.Vat)).ToList();
    }

    private static PettyCashFundDto ToDto(
        FundRow fund,
        List<(Guid FundId, PettyCashDocState State, int Count, decimal AmountBeforeTax, decimal Vat)> stats)
    {
        var approvedStats = stats.Where(s => s.FundId == fund.ID && s.State == PettyCashDocState.Approved).ToList();
        var inFlightStats = stats.Where(s => s.FundId == fund.ID && InFlightStates.Contains(s.State)).ToList();

        var approvedAmount = approvedStats.Sum(s => s.AmountBeforeTax + s.Vat);
        var approvedCount = approvedStats.Sum(s => s.Count);
        var inFlightAmount = inFlightStats.Sum(s => s.AmountBeforeTax + s.Vat);
        var inFlightCount = inFlightStats.Sum(s => s.Count);

        // §2: موجودی نقد = CEILING − Σ(مبلغ کل اسناد در New/PendingReview/Returned/Approved).
        var cashBalance = fund.CEILING - approvedAmount - inFlightAmount;

        return new PettyCashFundDto(
            fund.ID,
            fund.CODE,
            fund.NAME,
            fund.CUSTODIAN_USERID,
            fund.CUSTODIAN_NAME,
            fund.CEILING,
            fund.PER_DOC_LIMIT,
            fund.ALERT_THRESHOLD_PERCENT,
            fund.ACCOUNTCODE_ID,
            fund.AccountCodeTitle,
            fund.SETTLEMENT_PERIOD,
            fund.IS_ACTIVE,
            fund.ISDELETED,
            cashBalance,
            approvedAmount,
            approvedCount,
            inFlightAmount,
            inFlightCount);
    }
}
