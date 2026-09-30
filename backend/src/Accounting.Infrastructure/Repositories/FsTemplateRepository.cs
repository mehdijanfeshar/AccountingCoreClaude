using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class FsTemplateRepository : IFsTemplateRepository
{
    private readonly LegacyDbContext _dbContext;

    public FsTemplateRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddTemplateAsync(TB_FS_TEMPLATE template, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_TEMPLATEs.AddAsync(template, cancellationToken);
    }

    public async Task AddVersionAsync(TB_FS_TEMPLATE_VERSION version, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_TEMPLATE_VERSIONs.AddAsync(version, cancellationToken);
    }

    public async Task AddRowsAsync(IEnumerable<TB_FS_TEMPLATE_ROW> rows, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_TEMPLATE_ROWs.AddRangeAsync(rows, cancellationToken);
    }

    public void RemoveRows(IEnumerable<TB_FS_TEMPLATE_ROW> rows)
    {
        _dbContext.TB_FS_TEMPLATE_ROWs.RemoveRange(rows);
    }

    public Task<TB_FS_TEMPLATE?> GetTemplateForUpdateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_FS_TEMPLATEs
            .FirstOrDefaultAsync(t => t.ID == templateId && !t.ISDELETED, cancellationToken);
    }

    public async Task<bool> TemplateCodeExistsAsync(string? ownerVahedCode, string code, CancellationToken cancellationToken = default)
    {
        // CountAsync، نه AnyAsync — AnyAsync روی Oracle ORA-00904 می‌دهد (CLAUDE.md).
        var query = _dbContext.TB_FS_TEMPLATEs.Where(t => t.CODE == code);
        query = ownerVahedCode is null ? query.Where(t => t.VAHEDCODE == null) : query.Where(t => t.VAHEDCODE == ownerVahedCode);
        return await query.CountAsync(cancellationToken) > 0;
    }

    public Task<TB_FS_TEMPLATE_VERSION?> GetVersionForUpdateAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_FS_TEMPLATE_VERSIONs
            .Include(v => v.TEMPLATE)
            .FirstOrDefaultAsync(v => v.ID == versionId && !v.ISDELETED && !v.TEMPLATE.ISDELETED, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_FS_TEMPLATE_VERSION>> GetVersionsForUpdateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_FS_TEMPLATE_VERSIONs
            .Where(v => v.TEMPLATE_ID == templateId && !v.ISDELETED)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetMaxVersionNoAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_FS_TEMPLATE_VERSIONs
            .Where(v => v.TEMPLATE_ID == templateId)
            .Select(v => (int?)v.VERSION_NO)
            .MaxAsync(cancellationToken) ?? 0;
    }

    public async Task<IReadOnlyList<TB_FS_TEMPLATE_ROW>> GetRowsForUpdateAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_FS_TEMPLATE_ROWs
            .Where(r => r.VERSION_ID == versionId)
            .OrderBy(r => r.ORDER_NO)
            .ToListAsync(cancellationToken);
    }
}
