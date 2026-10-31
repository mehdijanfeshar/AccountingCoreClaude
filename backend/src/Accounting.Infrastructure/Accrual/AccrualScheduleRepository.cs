using Accounting.Application.Accrual;
using Accounting.Domain.Accrual;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Accrual;

/// <summary>حسابداری تعهدی (DDL 090). فقط stage؛ SaveChanges با هندلر. روی اوراکل AnyAsync نه.</summary>
public sealed class AccrualScheduleRepository : IAccrualScheduleRepository
{
    private readonly LegacyDbContext _db;
    public AccrualScheduleRepository(LegacyDbContext db) => _db = db;

    private IQueryable<AccrualSchedule> Live(bool tracking)
    {
        var q = _db.Set<AccrualSchedule>().Where(x => !x.IsDeleted);
        return tracking ? q : q.AsNoTracking();
    }

    public async Task AddRangeAsync(IEnumerable<AccrualSchedule> rows, CancellationToken ct) =>
        await _db.Set<AccrualSchedule>().AddRangeAsync(rows, ct);

    public async Task<AccrualSchedule?> LockAsync(Guid id, bool skipLocked, CancellationToken ct)
    {
        // فقط اوراکل و فقط داخل تراکنش؛ قفل با commit/rollback آزاد می‌شود. SQLite تست‌ها قفل ندارد (ریسک #۲۵).
        // شناسه ممکن است حروف بزرگ داشته باشد (ریسک #۱۱)، پس هر دو شکل.
        if (_db.Database.CurrentTransaction is not null
            && _db.Database.ProviderName?.Contains("Oracle", StringComparison.OrdinalIgnoreCase) == true)
        {
            var key = id.ToString("D");
            var sql = "SELECT ID AS \"Value\" FROM TB_AC_SCHEDULE WHERE ID IN ({0}, {1}) AND ISDELETED = 0 FOR UPDATE"
                      + (skipLocked ? " SKIP LOCKED" : "");
            var locked = await _db.Database.SqlQueryRaw<string>(sql, key.ToLowerInvariant(), key.ToUpperInvariant()).ToListAsync(ct);
            if (locked.Count == 0) return null;
        }

        var row = await Live(tracking: true).FirstOrDefaultAsync(x => x.Id == id, ct);
        // قفل گرفته شد ⇒ مقدار فعلی پایگاه، نه نسخهٔ قدیمی tracked همین scope.
        if (row is not null) await _db.Entry(row).ReloadAsync(ct);
        return row;
    }

    public async Task<IReadOnlyList<AccrualSchedule>> ListAsync(string vahedCode, AccrualScheduleFilter f, CancellationToken ct)
    {
        var q = Live(tracking: false).Where(x => x.VahedCode == vahedCode);
        if (f.State is { } state) q = q.Where(x => x.State == state);
        if (f.Kind is { } kind) q = q.Where(x => x.Kind == kind);
        if (f.FromDue is { } from) q = q.Where(x => string.Compare(x.DueDate, from) >= 0);
        if (f.ToDue is { } to) q = q.Where(x => string.Compare(x.DueDate, to) <= 0);
        return await q.OrderBy(x => x.DueDate).ThenBy(x => x.SourceVoucherId).ThenBy(x => x.LineNo).Take(f.Max).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AccrualSchedule>> ListBySourceAsync(string vahedCode, Guid sourceVoucherId, bool tracking, CancellationToken ct) =>
        await Live(tracking).Where(x => x.VahedCode == vahedCode && x.SourceVoucherId == sourceVoucherId)
            .OrderBy(x => x.Kind).ThenBy(x => x.LineNo).ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> ListDueIdsAsync(string todayJalali, int max, CancellationToken ct) =>
        await Live(tracking: false)
            .Where(x => x.State == AccrualScheduleState.Open && string.Compare(x.DueDate, todayJalali) <= 0)
            .OrderBy(x => x.DueDate).ThenBy(x => x.LineNo)
            .Select(x => x.Id).Take(max).ToListAsync(ct);

    public async Task<IReadOnlyDictionary<string, AccrualOpenCount>> CountOpenDueAsync(string lastDayJalali, CancellationToken ct)
    {
        var rows = await Live(tracking: false)
            .Where(x => x.State == AccrualScheduleState.Open && string.Compare(x.DueDate, lastDayJalali) <= 0)
            .GroupBy(x => new { x.VahedCode, x.Kind })
            .Select(g => new { g.Key.VahedCode, g.Key.Kind, Count = g.Count() })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.VahedCode, StringComparer.Ordinal).ToDictionary(
            g => g.Key,
            g => new AccrualOpenCount(
                g.Where(r => r.Kind == AccrualScheduleKind.Reversal).Sum(r => r.Count),
                g.Where(r => r.Kind != AccrualScheduleKind.Reversal).Sum(r => r.Count)),
            StringComparer.Ordinal);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetVoucherNumbersAsync(IReadOnlyCollection<Guid> voucherIds, CancellationToken ct)
    {
        var ids = voucherIds.Distinct().ToList();
        var rows = await _db.TB_VOUCHERSHEADs.AsNoTracking()
            .Where(h => ids.Contains(h.ID))
            .Select(h => new { h.ID, h.DOC_NUM })
            .ToListAsync(ct);
        return rows.Where(r => r.DOC_NUM != null).GroupBy(r => r.ID).ToDictionary(g => g.Key, g => g.First().DOC_NUM!);
    }
}
