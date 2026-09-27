using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
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
        var funds = await _dbContext.TB_REVOLVING_FUNDs
            .AsNoTracking()
            .Where(f => f.ISDELETED != true && f.VAHEDCODE == vahedCode)
            .Select(f => new
            {
                f.ID,
                f.CODE,
                f.NAME,
                f.DEFAULTAMOUNT,
                f.ACCOUNTCODE_ID,
                AccountCodeTitle = f.ACCOUNTCODE!.ACCCODENAME,
            })
            .ToListAsync(cancellationToken);

        if (funds.Count == 0)
        {
            return Array.Empty<PettyCashFundDto>();
        }

        var settings = await _dbContext.TB_PC_FUND_SETTINGs
            .AsNoTracking()
            .Where(s => !s.ISDELETED && s.VAHEDCODE == vahedCode)
            .ToListAsync(cancellationToken);

        // One grouped query for every fund's §2 balance-equation inputs, rather than one query
        // per fund — the funds list for a single unit is small, but this keeps it O(1) queries
        // regardless of how many funds that unit has.
        var docStats = await _dbContext.TB_PC_EXPENSE_DOCs
            .AsNoTracking()
            .Where(d => !d.ISDELETED && d.VAHEDCODE == vahedCode)
            .GroupBy(d => new { d.REVOLVINGFUND_ID, d.DOC_STATE })
            .Select(g => new
            {
                g.Key.REVOLVINGFUND_ID,
                g.Key.DOC_STATE,
                Count = g.Count(),
                AmountBeforeTax = g.Sum(x => (decimal?)x.AMOUNT_BEFORE_TAX) ?? 0m,
                Vat = g.Sum(x => (decimal?)x.VAT_AMOUNT) ?? 0m,
            })
            .ToListAsync(cancellationToken);

        var result = new List<PettyCashFundDto>(funds.Count);

        foreach (var fund in funds)
        {
            var setting = settings.FirstOrDefault(s => s.REVOLVINGFUND_ID == fund.ID);

            var approvedStats = docStats.Where(s => s.REVOLVINGFUND_ID == fund.ID && s.DOC_STATE == PettyCashDocState.Approved).ToList();
            var inFlightStats = docStats.Where(s => s.REVOLVINGFUND_ID == fund.ID && InFlightStates.Contains(s.DOC_STATE)).ToList();

            var approvedAmount = approvedStats.Sum(s => s.AmountBeforeTax + s.Vat);
            var approvedCount = approvedStats.Sum(s => s.Count);
            var inFlightAmount = inFlightStats.Sum(s => s.AmountBeforeTax + s.Vat);
            var inFlightCount = inFlightStats.Sum(s => s.Count);

            // §2: موجودی نقد = DEFAULTAMOUNT − Σ(مبلغ کل اسناد در New/PendingReview/Returned/Approved).
            var cashBalance = (fund.DEFAULTAMOUNT ?? 0m) - approvedAmount - inFlightAmount;

            result.Add(new PettyCashFundDto(
                fund.ID,
                fund.CODE,
                fund.NAME,
                fund.DEFAULTAMOUNT,
                fund.ACCOUNTCODE_ID,
                fund.AccountCodeTitle,
                setting is null
                    ? null
                    : new PettyCashFundSettingDto(
                        setting.CUSTODIAN_USERID,
                        setting.CUSTODIAN_NAME,
                        setting.PER_DOC_LIMIT,
                        setting.ALERT_THRESHOLD_PERCENT,
                        setting.SETTLEMENT_PERIOD),
                cashBalance,
                approvedAmount,
                approvedCount,
                inFlightAmount,
                inFlightCount));
        }

        return result;
    }
}
