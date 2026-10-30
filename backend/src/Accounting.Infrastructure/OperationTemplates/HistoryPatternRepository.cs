using System.Data.Common;
using Accounting.Application.OperationTemplates.History;
using Accounting.Domain.OperationTemplates;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>
/// ذخیره و جستجوی الگوهای سابقه (DDL 085). ORA-00942 (جدول نیست) ⇒ <see cref="HistoryPatternsUnavailableException"/> (۵۰۳).
/// روی Oracle از <c>AnyAsync</c> استفاده نمی‌شود (ORA-00904)؛ کلاس‌های member-init برای Select.
/// </summary>
public sealed class HistoryPatternRepository : IHistoryPatternStore
{
    private const int InChunk = 500;
    private const int InsertPatternChunk = 1000;
    private const int InsertUnitChunk = 2000;

    private readonly LegacyDbContext _db;
    public HistoryPatternRepository(LegacyDbContext db) => _db = db;

    public async Task ReplaceAllAsync(IReadOnlyList<HistoryPattern> patterns, IReadOnlyList<HistoryPatternUnit> units, CancellationToken ct)
    {
        await Guard(async () =>
        {
            // ترتیب: فرزند، سپس والد (FK). داخل تراکنش هندلر اجرا می‌شود.
            await _db.Set<HistoryPatternUnit>().ExecuteDeleteAsync(ct);
            await _db.Set<HistoryPattern>().ExecuteDeleteAsync(ct);

            foreach (var chunk in patterns.Chunk(InsertPatternChunk))
            {
                _db.Set<HistoryPattern>().AddRange(chunk);
                await _db.SaveChangesAsync(ct);
                _db.ChangeTracker.Clear();
            }
            foreach (var chunk in units.Chunk(InsertUnitChunk))
            {
                _db.Set<HistoryPatternUnit>().AddRange(chunk);
                await _db.SaveChangesAsync(ct);
                _db.ChangeTracker.Clear();
            }
            return 0;
        });
    }

    public async Task<IReadOnlySet<string>> DismissedSignaturesAsync(CancellationToken ct)
    {
        try
        {
            return (await _db.Set<HistoryDismissed>().AsNoTracking().Select(d => d.Signature).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
        }
        catch (DbException ex) when (ex.Message.Contains("ORA-00942", StringComparison.Ordinal))
        {
            return new HashSet<string>(); // 086 اجرا نشده ⇒ بازسازی کار می‌کند
        }
    }

    public async Task ClearDismissedAsync(CancellationToken ct)
    {
        try
        {
            await _db.Set<HistoryDismissed>().ExecuteDeleteAsync(ct);
        }
        catch (DbException ex) when (ex.Message.Contains("ORA-00942", StringComparison.Ordinal))
        {
            // 086 اجرا نشده ⇒ چیزی برای پاک‌کردن نیست
        }
    }

    public async Task DismissAsync(HistoryPattern pattern, string userId, CancellationToken ct)
    {
        try
        {
            _db.Set<HistoryDismissed>().Add(new HistoryDismissed { Signature = pattern.Signature, AddUserId = userId, CreatedAtUtc = DateTime.UtcNow });
            await _db.SaveChangesAsync(ct);
        }
        catch (DbException ex) when (ex.Message.Contains("ORA-00942", StringComparison.Ordinal))
        {
            throw new HistoryPatternsUnavailableException(tableMissing: true, script: "۰۸۶");
        }
        var id = pattern.Id;
        await Guard(async () =>
        {
            await _db.Set<HistoryPatternUnit>().Where(u => u.PatternId == id).ExecuteDeleteAsync(ct);
            await _db.Set<HistoryPattern>().Where(p => p.Id == id).ExecuteDeleteAsync(ct);
            return 0;
        });
    }

    public Task<int> CountAsync(CancellationToken ct) => Guard(() => _db.Set<HistoryPattern>().AsNoTracking().CountAsync(ct));

    private IQueryable<HistoryPattern> Filtered(HistoryPatternFilter f)
    {
        var q = _db.Set<HistoryPattern>().AsNoTracking().AsQueryable();
        if (f.MinCount > 0) q = q.Where(p => p.VoucherCount >= f.MinCount);
        if (f.FromYear is { } fy) q = q.Where(p => p.ToYear >= fy);
        if (f.ToYear is { } ty) q = q.Where(p => p.FromYear <= ty);
        if (!string.IsNullOrWhiteSpace(f.VahedType))
        {
            var needle = "," + f.VahedType + ",";
            q = q.Where(p => ("," + p.VahedTypes + ",").Contains(needle));
        }
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var raw = f.Search!;
            var norm = HistoryText.Normalize(raw).Trim();
            q = norm.Length == 0
                ? q.Where(p => p.SampleDescsJson!.Contains(raw))
                : q.Where(p => p.KeywordsJson!.Contains(norm) || p.SampleDescsJson!.Contains(raw));
        }

        return q;
    }

    public Task<HistoryPatternPage> ListAsync(HistoryPatternFilter f, CancellationToken ct) => Guard(async () =>
    {
        var q = Filtered(f);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.VoucherCount).ThenByDescending(p => p.LastDate)
            .Skip(f.Skip).Take(f.Take).ToListAsync(ct);
        return new HistoryPatternPage(items, total);
    });

    public async Task<(int Count, int SkippedCovered)> DismissUncoveredAsync(HistoryPatternFilter filter, IReadOnlySet<string> coveredSignatures, string userId, CancellationToken ct)
    {
        var rows = await Guard(() => Filtered(filter).Select(p => new { p.Id, p.Signature }).ToListAsync(ct));
        var targets = rows.Where(r => !coveredSignatures.Contains(r.Signature)).ToList();
        var skipped = rows.Count - targets.Count;
        if (targets.Count == 0) return (0, skipped);

        try
        {
            var existing = new HashSet<string>(StringComparer.Ordinal);
            foreach (var chunk in targets.Select(t => t.Signature).Chunk(InChunk))
            {
                var part = chunk.ToList();
                existing.UnionWith(await _db.Set<HistoryDismissed>().AsNoTracking().Where(d => part.Contains(d.Signature)).Select(d => d.Signature).ToListAsync(ct));
            }
            var now = DateTime.UtcNow;
            _db.Set<HistoryDismissed>().AddRange(targets.Where(t => !existing.Contains(t.Signature))
                .Select(t => new HistoryDismissed { Signature = t.Signature, AddUserId = userId, CreatedAtUtc = now }));
            await _db.SaveChangesAsync(ct);
        }
        catch (DbException ex) when (ex.Message.Contains("ORA-00942", StringComparison.Ordinal))
        {
            throw new HistoryPatternsUnavailableException(tableMissing: true, script: "۰۸۶");
        }

        await Guard(async () =>
        {
            foreach (var chunk in targets.Select(t => t.Id).Chunk(InChunk))
            {
                var part = chunk.ToList();
                await _db.Set<HistoryPatternUnit>().Where(u => part.Contains(u.PatternId)).ExecuteDeleteAsync(ct);
                await _db.Set<HistoryPattern>().Where(p => part.Contains(p.Id)).ExecuteDeleteAsync(ct);
            }
            return 0;
        });
        return (targets.Count, skipped);
    }

    public Task<HistoryPattern?> GetAsync(Guid id, CancellationToken ct) =>
        Guard(() => _db.Set<HistoryPattern>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct));

    public Task<IReadOnlyList<HistoryPattern>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) => Guard(async () =>
    {
        var result = new List<HistoryPattern>();
        foreach (var chunk in ids.Distinct().Chunk(InChunk))
        {
            var part = chunk.ToList();
            result.AddRange(await _db.Set<HistoryPattern>().AsNoTracking().Where(p => part.Contains(p.Id)).ToListAsync(ct));
        }
        return (IReadOnlyList<HistoryPattern>)result;
    });

    public Task<HistoryCandidates> FindCandidatesAsync(IReadOnlyCollection<string> tokens, string vahedCode, int take, IReadOnlyCollection<Guid> extraIds, CancellationToken ct) => Guard(async () =>
    {
        // سبک: بدون LinesJson (CLOB). ترتیب کاندیدا با امتیاز در Application تعیین می‌شود.
        var found = new Dictionary<Guid, HistoryPattern>();
        var own = new Dictionary<Guid, HistoryPatternUnit>();

        foreach (var token in tokens.Distinct())
        {
            var t = token;
            var globalHits = await _db.Set<HistoryPattern>().AsNoTracking()
                .Where(p => p.KeywordsJson != null && p.KeywordsJson.Contains(t))
                .OrderByDescending(p => p.VoucherCount).Take(take)
                .Select(p => new HistoryPattern
                {
                    Id = p.Id, Signature = p.Signature, VoucherCount = p.VoucherCount, UnitCount = p.UnitCount, Years = p.Years,
                    KeywordsJson = p.KeywordsJson, SampleDescsJson = p.SampleDescsJson,
                })
                .ToListAsync(ct);
            foreach (var p in globalHits) found.TryAdd(p.Id, p);

            var unitHits = await _db.Set<HistoryPatternUnit>().AsNoTracking()
                .Where(u => u.VahedCode == vahedCode && u.KeywordsJson != null && u.KeywordsJson.Contains(t))
                .OrderByDescending(u => u.VoucherCount).Take(take)
                .ToListAsync(ct);
            foreach (var u in unitHits) own.TryAdd(u.PatternId, u);
        }

        // الگوهای فقط-با-سابقهٔ-واحد یا فقط-معنایی که در فهرست کلی نبودند
        var missing = own.Keys.Concat(extraIds).Distinct().Where(id => !found.ContainsKey(id)).ToList();
        foreach (var chunk in missing.Chunk(InChunk))
        {
            var part = chunk.ToList();
            var rows = await _db.Set<HistoryPattern>().AsNoTracking().Where(p => part.Contains(p.Id))
                .Select(p => new HistoryPattern
                {
                    Id = p.Id, Signature = p.Signature, VoucherCount = p.VoucherCount, UnitCount = p.UnitCount, Years = p.Years,
                    KeywordsJson = p.KeywordsJson, SampleDescsJson = p.SampleDescsJson,
                }).ToListAsync(ct);
            foreach (var p in rows) found.TryAdd(p.Id, p);
        }

        // ردیف واحد کاربر برای همهٔ کاندیداها (سابقهٔ خود واحد حتی اگر کلمه‌اش با جمله نخورد، شمارش «در واحد من» را می‌دهد)
        var rest = found.Keys.Where(id => !own.ContainsKey(id)).ToList();
        foreach (var chunk in rest.Chunk(InChunk))
        {
            var part = chunk.ToList();
            foreach (var u in await _db.Set<HistoryPatternUnit>().AsNoTracking()
                         .Where(u => u.VahedCode == vahedCode && part.Contains(u.PatternId)).ToListAsync(ct))
                own.TryAdd(u.PatternId, u);
        }

        return new HistoryCandidates(found.Values.ToList(), own);
    });

    public Task<IReadOnlyDictionary<Guid, string>> GroupTitlesAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken ct) => Guard(async () =>
    {
        var result = new Dictionary<Guid, string>();
        foreach (var chunk in groupIds.Distinct().Chunk(InChunk))
        {
            var part = chunk.ToList();
            var rows = await _db.TB_TAFSIL_GROUPs.AsNoTracking().Where(g => part.Contains(g.ID))
                .Select(g => new { g.ID, g.TAFSILGROUP_NAME }).ToListAsync(ct);
            foreach (var r in rows) result[r.ID] = r.TAFSILGROUP_NAME;
        }
        return (IReadOnlyDictionary<Guid, string>)result;
    });

    public Task<IReadOnlySet<Guid>> FillableGroupsAsync(IReadOnlyCollection<Guid> groupIds, string vahedCode, CancellationToken ct) => Guard(async () =>
    {
        var typeCode = await _db.TB_VAHED_INFOs.AsNoTracking()
            .Where(v => v.VAHEDCODE == vahedCode)
            .Select(v => v.VAHEDTYPE.TYPECODE)
            .FirstOrDefaultAsync(ct);
        var callerCategory = (short)VahedCategoryMapper.FromTypeCode(typeCode);
        var allCategory = (short)VahedCategory.All;

        // قاعدهٔ B مثل DetailReader: لینک خود واحد، یا «همه»، یا دستهٔ واحد. فقط تفصیلی حذف‌نشده.
        var result = new HashSet<Guid>();
        foreach (var chunk in groupIds.Distinct().Chunk(InChunk))
        {
            var part = chunk.ToList();
            var hits = await (from l in _db.TB_TAFSIL_LINK_TAFSILGROUPs.AsNoTracking()
                              join t in _db.TB_TAFSILIs.AsNoTracking() on l.TAFSIL_ID equals t.ID
                              where part.Contains(l.TAFSILGROUP_ID) && l.ISDELETED == false && t.ISDELETED != true
                                    && (l.VAHEDCODE == vahedCode || l.VAHEDTYPE == allCategory || l.VAHEDTYPE == callerCategory)
                              select l.TAFSILGROUP_ID).Distinct().ToListAsync(ct);
            foreach (var g in hits) result.Add(g);
        }
        return (IReadOnlySet<Guid>)result;
    });

    private static async Task<T> Guard<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (DbException ex) when (ex.Message.Contains("ORA-00942", StringComparison.Ordinal))
        {
            throw new HistoryPatternsUnavailableException(tableMissing: true);
        }
    }
}
