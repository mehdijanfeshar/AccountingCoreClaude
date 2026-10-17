using Accounting.Application.Sales;
using Accounting.Domain.Entity;
using Accounting.Domain.Sales;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Sales;

/// <summary>ماژول فروش (DDL 080). فقط stage؛ SaveChanges با هندلر. روی اوراکل AnyAsync نه — CountAsync.</summary>
public sealed class SalesRepository : ISalesRepository
{
    private readonly LegacyDbContext _db;
    public SalesRepository(LegacyDbContext db) => _db = db;

    private IQueryable<T> Q<T>(bool tracking) where T : class => tracking ? _db.Set<T>() : _db.Set<T>().AsNoTracking();

    // ─── تنظیمات ───
    public Task<SalesSetting?> GetSettingAsync(string vahedCode, bool tracking, CancellationToken ct) =>
        Q<SalesSetting>(tracking).FirstOrDefaultAsync(s => s.VahedCode == vahedCode, ct);

    public async Task AddSettingAsync(SalesSetting setting, CancellationToken ct) => await _db.Set<SalesSetting>().AddAsync(setting, ct);

    public async Task<IReadOnlyList<SalesAccountMap>> GetAccountMapsAsync(string vahedCode, bool tracking, CancellationToken ct) =>
        await Q<SalesAccountMap>(tracking).Where(m => m.VahedCode == vahedCode).ToListAsync(ct);

    public void RemoveAccountMaps(IEnumerable<SalesAccountMap> maps) => _db.Set<SalesAccountMap>().RemoveRange(maps);

    public async Task AddAccountMapAsync(SalesAccountMap map, CancellationToken ct) => await _db.Set<SalesAccountMap>().AddAsync(map, ct);

    public async Task<IReadOnlyList<SalesVatRate>> ListVatRatesAsync(bool tracking, CancellationToken ct) =>
        await Q<SalesVatRate>(tracking).Where(r => !r.IsDeleted).ToListAsync(ct);

    public async Task AddVatRateAsync(SalesVatRate rate, CancellationToken ct) => await _db.Set<SalesVatRate>().AddAsync(rate, ct);

    public async Task<IReadOnlyList<SalesUserRole>> ListRolesAsync(string vahedCode, string? userId, CancellationToken ct) =>
        await _db.Set<SalesUserRole>().AsNoTracking()
            .Where(r => r.VahedCode == vahedCode && (userId == null || r.UserId == userId)).ToListAsync(ct);

    public Task<SalesUserRole?> GetRoleForUpdateAsync(Guid id, CancellationToken ct) =>
        _db.Set<SalesUserRole>().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddRoleAsync(SalesUserRole role, CancellationToken ct) => await _db.Set<SalesUserRole>().AddAsync(role, ct);

    public void RemoveRole(SalesUserRole role) => _db.Set<SalesUserRole>().Remove(role);

    // ─── کالا ───
    public async Task<IReadOnlyList<SalesItem>> ListItemsAsync(string vahedCode, string? search, bool activeOnly, CancellationToken ct)
    {
        var q = _db.Set<SalesItem>().AsNoTracking().Where(i => i.VahedCode == vahedCode && !i.IsDeleted);
        if (activeOnly) q = q.Where(i => i.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(i => i.Code.Contains(s.ToUpper()) || i.Name.Contains(s));
        }
        return await q.OrderBy(i => i.Code).Take(1000).ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, SalesItem>> GetItemsAsync(IReadOnlyCollection<Guid> ids, bool tracking, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, SalesItem>();
        var list = ids.ToList();
        return await Q<SalesItem>(tracking).Where(i => list.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
    }

    public async Task<bool> ItemCodeExistsAsync(string vahedCode, string code, Guid? exceptId, CancellationToken ct) =>
        await _db.Set<SalesItem>().CountAsync(i => i.VahedCode == vahedCode && i.Code == code && !i.IsDeleted && (exceptId == null || i.Id != exceptId), ct) > 0;

    public async Task AddItemAsync(SalesItem item, CancellationToken ct) => await _db.Set<SalesItem>().AddAsync(item, ct);

    // ─── مشتری ───
    public async Task<IReadOnlyList<SalesCustomerRow>> ListCustomersAsync(string vahedCode, string? search, IReadOnlyCollection<Guid>? ids, CancellationToken ct)
    {
        var q = from c in _db.Set<SalesCustomer>().AsNoTracking()
                join t in _db.Set<TB_TAFSILI>().AsNoTracking() on c.TafsiliId equals t.ID into tj
                from t in tj.DefaultIfEmpty()
                where c.VahedCode == vahedCode && !c.IsDeleted
                select new { c, Code = t != null ? t.TAFSILI_CODE : null, Name = t != null ? t.TAFSILI_NAME : null };
        if (ids is not null)
        {
            var list = ids.ToList();
            q = q.Where(x => list.Contains(x.c.Id));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => (x.Name != null && x.Name.Contains(s)) || (x.Code != null && x.Code.Contains(s)) || (x.c.NationalId != null && x.c.NationalId.Contains(s)));
        }
        var rows = await q.OrderBy(x => x.Name).Take(1000).ToListAsync(ct);
        return rows.Select(x => new SalesCustomerRow(x.c, x.Code, x.Name)).ToList();
    }

    public Task<SalesCustomer?> GetCustomerAsync(Guid id, bool tracking, CancellationToken ct) =>
        Q<SalesCustomer>(tracking).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<bool> CustomerExistsForTafsiliAsync(string vahedCode, Guid tafsiliId, Guid? exceptId, CancellationToken ct) =>
        await _db.Set<SalesCustomer>().CountAsync(c => c.VahedCode == vahedCode && c.TafsiliId == tafsiliId && !c.IsDeleted && (exceptId == null || c.Id != exceptId), ct) > 0;

    public async Task<bool> NationalIdExistsAsync(string vahedCode, string nationalId, Guid? exceptId, CancellationToken ct) =>
        await _db.Set<SalesCustomer>().CountAsync(c => c.VahedCode == vahedCode && c.NationalId == nationalId && !c.IsDeleted && (exceptId == null || c.Id != exceptId), ct) > 0;

    public async Task AddCustomerAsync(SalesCustomer customer, CancellationToken ct) => await _db.Set<SalesCustomer>().AddAsync(customer, ct);

    // ─── اسناد ───
    public async Task<(IReadOnlyList<SalesDoc> Items, int Total)> ListDocsAsync(SalesDocFilter f, CancellationToken ct)
    {
        var q = _db.Set<SalesDoc>().AsNoTracking().Where(d => d.VahedCode == f.VahedCode && !d.IsDeleted);
        if (f.Type is { } t) q = q.Where(d => d.DocType == t);
        if (f.Status is { } st) q = q.Where(d => d.Status == st);
        if (f.CustomerId is { } c) q = q.Where(d => d.CustomerId == c);
        if (!string.IsNullOrWhiteSpace(f.FromDate)) q = q.Where(d => string.Compare(d.DocDate, f.FromDate) >= 0);
        if (!string.IsNullOrWhiteSpace(f.ToDate)) q = q.Where(d => string.Compare(d.DocDate, f.ToDate) <= 0);
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search;
            var customerIds = from cu in _db.Set<SalesCustomer>()
                              join ta in _db.Set<TB_TAFSILI>() on cu.TafsiliId equals ta.ID
                              where cu.VahedCode == f.VahedCode && ta.TAFSILI_NAME != null && ta.TAFSILI_NAME.Contains(s)
                              select cu.Id;
            q = q.Where(d => (d.DocNo != null && d.DocNo.Contains(s.ToUpper())) || (d.RefNo != null && d.RefNo.Contains(s)) || customerIds.Contains(d.CustomerId));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(d => d.DocDate).ThenByDescending(d => d.CreatedAtUtc).Skip(f.Skip).Take(f.Take).ToListAsync(ct);
        return (items, total);
    }

    public Task<SalesDoc?> GetDocAsync(Guid id, bool tracking, CancellationToken ct) =>
        Q<SalesDoc>(tracking).Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<SalesDoc>> GetChildrenAsync(IReadOnlyCollection<Guid> sourceIds, CancellationToken ct)
    {
        if (sourceIds.Count == 0) return [];
        var ids = sourceIds.ToList();
        return await _db.Set<SalesDoc>().AsNoTracking().Include(d => d.Lines)
            .Where(d => d.SourceId != null && ids.Contains(d.SourceId.Value) && !d.IsDeleted && d.Status != SalesDocStatus.Void)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SalesDoc>> GetApprovedOrdersAsync(Guid customerId, CancellationToken ct) =>
        await _db.Set<SalesDoc>().AsNoTracking().Include(d => d.Lines)
            .Where(d => d.CustomerId == customerId && d.DocType == SalesDocType.Order && d.Status == SalesDocStatus.Approved && !d.IsDeleted)
            .ToListAsync(ct);

    public async Task AddDocAsync(SalesDoc doc, CancellationToken ct) => await _db.Set<SalesDoc>().AddAsync(doc, ct);

    public void AddLines(IEnumerable<SalesDocLine> lines) => _db.Set<SalesDocLine>().AddRange(lines);

    public void RemoveLines(IEnumerable<SalesDocLine> lines) => _db.Set<SalesDocLine>().RemoveRange(lines);

    public async Task<int> MaxDocSequenceAsync(string vahedCode, string year, SalesDocType type, CancellationToken ct)
    {
        var numbers = await _db.Set<SalesDoc>().AsNoTracking()
            .Where(d => d.VahedCode == vahedCode && d.Year == year && d.DocType == type && d.DocNo != null)
            .Select(d => d.DocNo!).ToListAsync(ct);
        return numbers.Select(n => int.TryParse(n[(n.LastIndexOf('-') + 1)..], out var v) ? v : 0).DefaultIfEmpty(0).Max();
    }

    public async Task AddHistoryAsync(SalesDocHistory history, CancellationToken ct) => await _db.Set<SalesDocHistory>().AddAsync(history, ct);

    public async Task<IReadOnlyList<SalesDocHistory>> ListHistoryAsync(Guid docId, CancellationToken ct) =>
        await _db.Set<SalesDocHistory>().AsNoTracking().Where(h => h.DocId == docId).OrderByDescending(h => h.CreatedAtUtc).ToListAsync(ct);
}

/// <summary>مانده دریافتنی مشتری از ردیف‌های سند (حذف‌نشده) روی حساب و تفصیلی، در یک سال مالی.</summary>
public sealed class SalesLedgerReader : ISalesLedgerReader
{
    private readonly LegacyDbContext _db;
    public SalesLedgerReader(LegacyDbContext db) => _db = db;

    public async Task<long> ReceivableBalanceAsync(string vahedCode, string year, Guid accountId, Guid tafsiliId, CancellationToken ct)
    {
        var sum = await (from d in _db.Set<TB_VOUCHERSDETAIL>().AsNoTracking()
                         join h in _db.Set<TB_VOUCHERSHEAD>().AsNoTracking() on d.VOUCHERSHEAD_ID equals h.ID
                         join l in _db.Set<TB_VOUCHERDETAIL_LINK_TAFSILI>().AsNoTracking() on d.ID equals l.VOUCHERSDETAIL_ID
                         where d.VAHEDCODE == vahedCode && d.YEAR == year && d.ACCOUNT_ID == accountId && l.TAFSILI_ID == tafsiliId
                               && d.ISDELETED != true && h.ISDELETED != true && !l.ISDELETED
                         select (d.DEBTOR ?? 0) - (d.CREDITOR ?? 0)).SumAsync(ct);
        return (long)sum;
    }
}
