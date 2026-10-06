using Accounting.Application.OperationTemplates;
using Accounting.Domain.OperationTemplates;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>گزارش‌های ذخیره‌شدهٔ حسابیار (DDL 072). فقط stage؛ SaveChanges با هندلر. (AnyAsync روی Oracle ممنوع.)</summary>
public sealed class SavedReportRepository : ISavedReportRepository
{
    private readonly LegacyDbContext _db;
    public SavedReportRepository(LegacyDbContext db) => _db = db;

    public async Task<IReadOnlyList<SavedReport>> ListAsync(bool activeOnly, CancellationToken ct)
    {
        var q = _db.Set<SavedReport>().AsNoTracking();
        if (activeOnly) q = q.Where(r => r.IsActive);
        return await q.OrderBy(r => r.Title).ToListAsync(ct);
    }

    public Task<SavedReport?> GetAsync(Guid id, CancellationToken ct) =>
        _db.Set<SavedReport>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<SavedReport?> GetForUpdateAsync(Guid id, CancellationToken ct) =>
        _db.Set<SavedReport>().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<bool> CodeExistsForOtherAsync(string code, Guid? exceptId, CancellationToken ct) =>
        await _db.Set<SavedReport>().CountAsync(r => r.Code == code && (exceptId == null || r.Id != exceptId), ct) > 0;

    public async Task AddAsync(SavedReport report, CancellationToken ct) =>
        await _db.Set<SavedReport>().AddAsync(report, ct);
}
