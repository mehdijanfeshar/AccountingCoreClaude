using Accounting.Application.AuditLogs;
using Accounting.Application.Common;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>خواندن لاگ تغییرات (<c>TB_AUDITLOG</c>) — فقط خواندن، بدون هیچ مسیر اصلاح یا حذف.</summary>
public sealed class AuditLogReadRepository : IAuditLogReadRepository
{
    private const int InListChunk = 900;

    private readonly LegacyDbContext _db;

    public AuditLogReadRepository(LegacyDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogDto>> SearchAsync(
        AuditLogFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // سقف IN اوراکل ۱۰۰۰ است؛ دامنهٔ بزرگ‌تر با UNION ALL بخش‌ها (واحدها مجزا، پس تکرار ندارد).
        IQueryable<Domain.Entity.TB_AUDITLOG>? q = null;
        foreach (var chunk in filter.VahedCodes.Distinct().Chunk(InListChunk))
        {
            var part = _db.TB_AUDITLOGs.AsNoTracking().Where(a => a.VAHEDCODE != null && chunk.Contains(a.VAHEDCODE));
            q = q is null ? part : q.Concat(part);
        }

        if (q is null)
            return new PagedResult<AuditLogDto> { PageNumber = pageNumber, PageSize = pageSize };

        if (filter.FromDate is { } from)
            q = q.Where(a => a.CHANGEDATE >= from);
        if (filter.ToDate is { } to)
            q = q.Where(a => a.CHANGEDATE < to);
        if (filter.UserId is { } user)
            q = q.Where(a => a.USERID == user);
        if (filter.TableName is { } table)
            q = q.Where(a => a.TABLENAME == table);
        if (filter.ActionType is { } action)
            q = q.Where(a => a.ACTIONTYPE == action);
        if (filter.RecordId is { } record)
            q = q.Where(a => a.RECORDID == record || a.PARENTID == record);
        if (filter.OperationId is { } op)
            q = q.Where(a => a.OPERATIONID == op);
        if (filter.Search is { } search)
            q = q.Where(a => (a.RECORDLABEL != null && a.RECORDLABEL.Contains(search))
                             || (a.USERFULLNAME != null && a.USERFULLNAME.Contains(search)));

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(a => a.CHANGEDATE).ThenBy(a => a.OPERATIONID).ThenBy(a => a.TABLENAME).ThenBy(a => a.COLUMNNAME)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = rows.Select(a => new AuditLogDto(
                a.ID,
                a.CHANGEDATE is { } d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null,
                a.ACTIONTYPE,
                a.TABLENAME,
                AuditTableCatalog.TitleOf(a.TABLENAME),
                a.RECORDID,
                a.RECORDLABEL,
                a.PARENTID,
                a.COLUMNNAME,
                a.OLDVALUE,
                a.NEWVALUE,
                a.USERID,
                a.USERFULLNAME,
                a.VAHEDCODE,
                a.YEAR,
                a.IPADDRESS,
                a.OPERATIONID)).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
        };
    }
}
