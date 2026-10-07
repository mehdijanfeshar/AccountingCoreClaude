using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary><see cref="IMonthCloseRepository"/>. تاریخ سند و اعلامیه yyyyMMdd است ⇒ تطبیق پیشوند yyyyMM.</summary>
public sealed class MonthCloseRepository : IMonthCloseRepository
{
    // Oracle IN-list limit is 1000 expressions.
    private const int InListChunk = 500;

    private static readonly byte?[] UnsentElamStates = [1, 2, 3, 5, 6, 7];

    private readonly LegacyDbContext _db;

    public MonthCloseRepository(LegacyDbContext db)
    {
        _db = db;
    }

    private static string Prefix(string year, int month) => $"{year}{month:00}";

    public async Task<IReadOnlyList<UnitDocLifeCount>> GetVoucherCountsAsync(string year, int month, CancellationToken cancellationToken = default)
    {
        var prefix = Prefix(year, month);
        var rows = await _db.TB_VOUCHERSHEADs
            .Where(h => h.YEAR == year && h.ISDELETED != true && h.VAHEDCODE != null
                        && h.DATE_DOC != null && h.DATE_DOC.StartsWith(prefix))
            .GroupBy(h => new { h.VAHEDCODE, h.DOCLIFE })
            .Select(g => new { g.Key.VAHEDCODE, g.Key.DOCLIFE, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new UnitDocLifeCount(r.VAHEDCODE!, r.DOCLIFE, r.Count)).ToList();
    }

    public async Task<IReadOnlyDictionary<string, int>> GetUnsentElamCountsAsync(string year, int month, CancellationToken cancellationToken = default)
    {
        var prefix = Prefix(year, month);
        var rows = await _db.TB_ELAMHEADs
            .Where(e => e.ISDELETED != true && e.VAHEDCODE != null && UnsentElamStates.Contains(e.WEB_STAT)
                        && e.ELAMH_DATE != null && e.ELAMH_DATE.StartsWith(prefix))
            .GroupBy(e => e.VAHEDCODE)
            .Select(g => new { VahedCode = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.VahedCode!, r => r.Count, StringComparer.Ordinal);
    }

    public async Task<int> AcceptReviewedAsync(string year, int month, IReadOnlyCollection<string> vahedCodes, string userId, CancellationToken cancellationToken = default)
    {
        var prefix = Prefix(year, month);
        var now = DateTime.UtcNow;
        var total = 0;
        foreach (var chunk in vahedCodes.Chunk(InListChunk))
        {
            total += await _db.TB_VOUCHERSHEADs
                .Where(h => h.YEAR == year && h.ISDELETED != true && chunk.Contains(h.VAHEDCODE!)
                            && h.DOCLIFE == DocLife.Reviewed && h.DATE_DOC != null && h.DATE_DOC.StartsWith(prefix))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(h => h.DOCLIFE, DocLife.Accepted)
                    .SetProperty(h => h.CHANGEUSERID, userId)
                    .SetProperty(h => h.UPDATEDDATE, now), cancellationToken);
        }
        return total;
    }

    public async Task AddLogsAsync(IEnumerable<TB_MONTH_CLOSE_LOG> logs, CancellationToken cancellationToken = default)
        => await _db.TB_MONTH_CLOSE_LOGs.AddRangeAsync(logs, cancellationToken);

    public async Task<IReadOnlyList<TB_MONTH_CLOSE_LOG>> GetLogsAsync(string year, int? month, string? vahedCode, CancellationToken cancellationToken = default)
    {
        var q = _db.TB_MONTH_CLOSE_LOGs.AsNoTracking().Where(l => l.YEAR == year);
        if (month is not null)
            q = q.Where(l => l.MONTH == month);
        if (vahedCode is not null)
            q = q.Where(l => l.VAHEDCODE == vahedCode);
        return await q.OrderByDescending(l => l.CREATEDDATE).ThenBy(l => l.VAHEDCODE).Take(5000).ToListAsync(cancellationToken);
    }
}
