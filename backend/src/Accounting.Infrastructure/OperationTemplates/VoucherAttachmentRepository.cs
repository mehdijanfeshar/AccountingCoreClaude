using Accounting.Application.OperationTemplates;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>
/// پیوست سندهای حسابیار روی <c>TB_ATTACH</c> (جدول Legacy برنامهٔ قدیم). فهرست هرگز ستون BLOB را نمی‌خواند.
/// فقط stage؛ SaveChanges با هندلر.
/// </summary>
public sealed class VoucherAttachmentRepository : IVoucherAttachmentRepository
{
    private readonly LegacyDbContext _db;
    public VoucherAttachmentRepository(LegacyDbContext db) => _db = db;

    public async Task<IReadOnlyList<VoucherAttachmentDto>> ListAsync(Guid voucherId, CancellationToken ct) =>
        await _db.Set<TB_ATTACH>().AsNoTracking()
            .Where(a => a.VOUCHERSHEAD_ID == voucherId && a.ISDELETED != true)
            .OrderBy(a => a.ATTACH_RADIF)
            .Select(a => new VoucherAttachmentDto(a.ID, a.ATTACH_NAME, a.TITLE, a.ATTACH_SIZE, a.CREATEDDATE, a.ADDUSERID))
            .ToListAsync(ct);

    public Task<TB_ATTACH?> FindAsync(Guid voucherId, Guid attachmentId, CancellationToken ct) =>
        _db.Set<TB_ATTACH>().FirstOrDefaultAsync(a => a.ID == attachmentId && a.VOUCHERSHEAD_ID == voucherId && a.ISDELETED != true, ct);

    public async Task<int> GetMaxRadifAsync(string vahedCode, string year, CancellationToken ct) =>
        await _db.Set<TB_ATTACH>().AsNoTracking()
            .Where(a => a.VAHEDCODE == vahedCode && a.YEAR == year)
            .MaxAsync(a => (int?)a.ATTACH_RADIF, ct) ?? 0;

    public async Task AddAsync(TB_ATTACH attachment, CancellationToken ct) =>
        await _db.Set<TB_ATTACH>().AddAsync(attachment, ct);

    public async Task<(string? Year, DocLife? Life)?> GetVoucherAsync(Guid voucherId, CancellationToken ct)
    {
        var h = await _db.Set<TB_VOUCHERSHEAD>().AsNoTracking()
            .Where(x => x.ID == voucherId && x.ISDELETED != true)
            .Select(x => new { x.YEAR, x.DOCLIFE })
            .FirstOrDefaultAsync(ct);
        return h is null ? null : (h.YEAR, h.DOCLIFE);
    }
}
