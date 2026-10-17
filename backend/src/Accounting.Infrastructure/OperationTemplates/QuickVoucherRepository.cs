using Accounting.Application.OperationTemplates;
using Accounting.Domain.OperationTemplates;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>سندهای پرتکرار حسابیار (DDL 079). فقط stage؛ SaveChanges با هندلر.</summary>
public sealed class QuickVoucherRepository : IQuickVoucherRepository
{
    private readonly LegacyDbContext _db;
    public QuickVoucherRepository(LegacyDbContext db) => _db = db;

    public async Task<IReadOnlyList<QuickVoucher>> ListAsync(string vahedCode, CancellationToken ct) =>
        await _db.Set<QuickVoucher>().AsNoTracking()
            .Where(q => q.VahedCode == vahedCode && !q.IsDeleted)
            .OrderBy(q => q.Title)
            .ToListAsync(ct);

    public Task<QuickVoucher?> GetForUpdateAsync(Guid id, CancellationToken ct) =>
        _db.Set<QuickVoucher>().FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted, ct);

    public async Task AddAsync(QuickVoucher quick, CancellationToken ct) =>
        await _db.Set<QuickVoucher>().AddAsync(quick, ct);
}
