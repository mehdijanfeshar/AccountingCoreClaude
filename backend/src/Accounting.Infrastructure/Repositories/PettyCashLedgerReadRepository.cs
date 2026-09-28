using System.Globalization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPettyCashLedgerReadRepository"/>. Every source
/// (ترمیم/صورت‌هزینه/استرداد) is fetched as its own plain, flat query — never a projection built
/// from a LeftJoin chain — per the module's own §۳-الف warning ("فیلتر/گروه‌بندی را روی جدول‌های
/// ساده انجام بده، نه projection رکوردی حاصل LeftJoin؛ روی Oracle شکسته بود"). The three lists are
/// merged and the running balance computed entirely client-side.
/// </summary>
public sealed class PettyCashLedgerReadRepository : IPettyCashLedgerReadRepository
{
    private sealed record RawRow(string Date, string Type, string Reference, string? Description, decimal Receipt, decimal Payment, Guid SourceId);

    private readonly LegacyDbContext _dbContext;

    public PettyCashLedgerReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PettyCashFundLedgerDto?> GetAsync(
        Guid fundId, string? from, string? to, string? type, string vahedCode, CancellationToken cancellationToken = default)
    {
        var fund = await _dbContext.TB_PC_FUNDs.AsNoTracking()
            .Where(f => f.ID == fundId && f.VAHEDCODE == vahedCode)
            .Select(f => new { f.CEILING })
            .FirstOrDefaultAsync(cancellationToken);

        if (fund is null)
        {
            return null;
        }

        var replenishmentRows = await _dbContext.TB_PC_REPLENISHMENTs.AsNoTracking()
            .Where(r => r.FUND_ID == fundId && r.VAHEDCODE == vahedCode && !r.ISDELETED
                        && r.STATE == PettyCashReplenishmentState.Paid)
            .Select(r => new { r.ID, r.CODE, r.NOTE, r.TOTAL_AMOUNT, r.PAID_DATE })
            .ToListAsync(cancellationToken);

        var expenseRows = await (
            from doc in _dbContext.TB_PC_EXPENSE_DOCs.AsNoTracking()
            where doc.FUND_ID == fundId && doc.VAHEDCODE == vahedCode && !doc.ISDELETED
                  && (doc.DOC_STATE == PettyCashDocState.Approved || doc.DOC_STATE == PettyCashDocState.Settled)
            join head in _dbContext.TB_CHARGEANDCOST_HEADs.AsNoTracking() on doc.CHARGEANDCOSTHEAD_ID equals head.ID
            select new
            {
                doc.ID,
                head.CHARGEANDCOST_CODE,
                head.CHARGEANDCOST_DATE,
                head.DESCRIPTION,
                doc.AMOUNT_BEFORE_TAX,
                doc.VAT_AMOUNT,
            })
            .ToListAsync(cancellationToken);

        var refundRows = await _dbContext.TB_PC_REFUNDs.AsNoTracking()
            .Where(r => r.FUND_ID == fundId && r.VAHEDCODE == vahedCode && !r.ISDELETED)
            .Select(r => new { r.ID, r.CODE, r.REASON, r.AMOUNT, r.REFUND_DATE })
            .ToListAsync(cancellationToken);

        var allRows = new List<RawRow>();

        allRows.AddRange(replenishmentRows.Select(r => new RawRow(
            ToPersianDate(r.PAID_DATE ?? DateTime.UtcNow), "replenishment", r.CODE, r.NOTE, r.TOTAL_AMOUNT, 0m, r.ID)));

        allRows.AddRange(expenseRows.Select(e => new RawRow(
            e.CHARGEANDCOST_DATE, "expense", "TH-" + e.CHARGEANDCOST_CODE, e.DESCRIPTION,
            0m, (e.AMOUNT_BEFORE_TAX ?? 0m) + (e.VAT_AMOUNT ?? 0m), e.ID)));

        allRows.AddRange(refundRows.Select(r => new RawRow(
            r.REFUND_DATE, "refund", r.CODE, r.REASON, r.AMOUNT, 0m, r.ID)));

        allRows = allRows.OrderBy(r => r.Date, StringComparer.Ordinal).ThenBy(r => r.Type, StringComparer.Ordinal).ToList();

        // «اولین تخصیص سقف» (تصمیم ۲۰۲۶-۰۹-۲۸) — CEILING همیشه پیش از هر بازهٔ درخواستی لحاظ
        // می‌شود، صرف نظر از این‌که چه زمانی تنخواه ساخته شده — رجوع به PettyCashFundLedgerDto XML doc.
        var beforeFrom = string.IsNullOrEmpty(from)
            ? Enumerable.Empty<RawRow>()
            : allRows.Where(r => string.CompareOrdinal(r.Date, from) < 0);

        var openingBalance = fund.CEILING + beforeFrom.Sum(r => r.Receipt) - beforeFrom.Sum(r => r.Payment);

        var inRange = allRows
            .Where(r => (string.IsNullOrEmpty(from) || string.CompareOrdinal(r.Date, from) >= 0)
                        && (string.IsNullOrEmpty(to) || string.CompareOrdinal(r.Date, to) <= 0))
            .ToList();

        // Running balance is computed over the FULL in-range set first (every type), so each
        // row's Balance is always the true point-in-time balance — the type filter below only
        // narrows which rows/totals are returned, never re-derives Balance from a partial set
        // (see PettyCashLedgerRowDto XML doc).
        var running = openingBalance;
        var rowsWithBalance = new List<PettyCashLedgerRowDto>(inRange.Count);

        foreach (var row in inRange)
        {
            running += row.Receipt - row.Payment;
            rowsWithBalance.Add(new PettyCashLedgerRowDto(
                row.Date,
                row.Type,
                row.Reference,
                row.Description,
                row.Receipt == 0m ? null : row.Receipt,
                row.Payment == 0m ? null : row.Payment,
                running,
                row.SourceId));
        }

        var closingBalance = running;

        var displayedRows = string.IsNullOrWhiteSpace(type)
            ? rowsWithBalance
            : rowsWithBalance.Where(r => string.Equals(r.Type, type, StringComparison.OrdinalIgnoreCase)).ToList();

        var totalReceipt = displayedRows.Sum(r => r.Receipt ?? 0m);
        var receiptCount = displayedRows.Count(r => r.Receipt is > 0m);
        var totalPayment = displayedRows.Sum(r => r.Payment ?? 0m);
        var paymentCount = displayedRows.Count(r => r.Payment is > 0m);

        return new PettyCashFundLedgerDto(openingBalance, displayedRows, totalReceipt, receiptCount, totalPayment, paymentCount, closingBalance);
    }

    /// <summary>
    /// Converts a Gregorian audit timestamp (<c>PAID_DATE</c>) to a شمسی <c>YYYYMMDD</c> string so
    /// it sorts/compares ordinally the same way as every other Persian date field in this module
    /// (<c>CHARGEANDCOST_DATE</c>, <c>REFUND_DATE</c>). No other place in this project needed this
    /// conversion before — every other date-range report filters a VARCHAR2(8) Persian column
    /// directly (see <c>GetAccountJournalQueryValidator</c>) — because <c>PAID_DATE</c> is the
    /// first <c>TIMESTAMP</c> column this module's design explicitly calls for (§۳-الف).
    /// </summary>
    private static string ToPersianDate(DateTime utc)
    {
        var calendar = new PersianCalendar();
        return $"{calendar.GetYear(utc):D4}{calendar.GetMonth(utc):D2}{calendar.GetDayOfMonth(utc):D2}";
    }
}
