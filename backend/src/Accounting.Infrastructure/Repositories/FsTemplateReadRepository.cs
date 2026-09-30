using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// قالب‌های صورت مالی، سمت خواندن. جدول قالب کوچک است (ده‌ها ردیف)، پس فیلتر مالکیت
/// (<see cref="FsUnitScope"/>) در حافظه انجام می‌شود، نه با <c>IN</c> روی فهرست واحدها.
/// </summary>
public sealed class FsTemplateReadRepository : IFsTemplateReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public FsTemplateReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<FsTemplateDto>> GetTemplatesAsync(FsFramework? framework, FsUnitScope scope, CancellationToken cancellationToken = default)
    {
        var templatesQuery = _dbContext.TB_FS_TEMPLATEs.AsNoTracking().Where(t => !t.ISDELETED);

        if (framework is { } fw)
        {
            templatesQuery = templatesQuery.Where(t => t.FRAMEWORK == fw);
        }

        var templates = (await templatesQuery.ToListAsync(cancellationToken))
            .Where(t => scope.CanSee(t.VAHEDCODE))
            .OrderBy(t => t.FRAMEWORK)
            .ThenBy(t => t.ORDER_NO)
            .ThenBy(t => t.CODE, StringComparer.Ordinal)
            .ThenBy(t => t.VAHEDCODE is null ? 1 : 0)
            .ToList();

        if (templates.Count == 0)
        {
            return Array.Empty<FsTemplateDto>();
        }

        var templateIds = templates.Select(t => t.ID).ToList();

        var versions = await _dbContext.TB_FS_TEMPLATE_VERSIONs
            .AsNoTracking()
            .Where(v => templateIds.Contains(v.TEMPLATE_ID) && !v.ISDELETED)
            .ToListAsync(cancellationToken);

        var versionIds = versions.Select(v => v.ID).ToList();

        var rowCounts = await _dbContext.TB_FS_TEMPLATE_ROWs
            .AsNoTracking()
            .Where(r => versionIds.Contains(r.VERSION_ID))
            .GroupBy(r => r.VERSION_ID)
            .Select(g => new { VersionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VersionId, x => x.Count, cancellationToken);

        var versionsByTemplate = versions.ToLookup(v => v.TEMPLATE_ID);

        return templates
            .Select(t => new FsTemplateDto(
                t.ID,
                t.VAHEDCODE,
                t.VAHEDCODE is null ? null : scope.Names.GetValueOrDefault(t.VAHEDCODE),
                scope.CanEdit(t.VAHEDCODE),
                t.FRAMEWORK,
                t.CODE,
                t.TITLE_FA,
                t.TITLE_EN,
                t.STATEMENT_TYPE,
                t.ORDER_NO,
                t.NOTE_PARENT_TEMPLATE_CODE,
                t.NOTE_PARENT_ROW_CODE,
                t.NOTE_TOTAL_ROW_CODE,
                versionsByTemplate[t.ID]
                    .OrderByDescending(v => v.VERSION_NO)
                    .Select(v => new FsTemplateVersionSummaryDto(
                        v.ID,
                        v.VERSION_NO,
                        v.STATE,
                        v.EFFECTIVE_FROM_YEAR,
                        v.DESCRIPTION,
                        v.ACTIVATED_BY,
                        v.ACTIVATED_DATE,
                        rowCounts.GetValueOrDefault(v.ID),
                        v.CREATEDDATE))
                    .ToList()))
            .ToList();
    }

    public async Task<FsTemplateVersionDetailDto?> GetVersionAsync(Guid versionId, FsUnitScope? scope, CancellationToken cancellationToken = default)
    {
        var version = await _dbContext.TB_FS_TEMPLATE_VERSIONs
            .AsNoTracking()
            .Include(v => v.TEMPLATE)
            .FirstOrDefaultAsync(v => v.ID == versionId && !v.ISDELETED && !v.TEMPLATE.ISDELETED, cancellationToken);

        if (version is null || (scope is not null && !scope.CanSee(version.TEMPLATE.VAHEDCODE)))
        {
            return null;
        }

        return await ToDetailAsync(version, scope?.CanEdit(version.TEMPLATE.VAHEDCODE) ?? false, cancellationToken);
    }

    public async Task<IReadOnlyList<FsTemplateVersionDetailDto>> GetVersionsForRunAsync(
        FsFramework framework, int year, bool useDrafts, FsUnitScope scope, CancellationToken cancellationToken = default)
    {
        var candidates = (await _dbContext.TB_FS_TEMPLATE_VERSIONs
                .AsNoTracking()
                .Include(v => v.TEMPLATE)
                .Where(v => !v.ISDELETED
                    && !v.TEMPLATE.ISDELETED
                    && v.TEMPLATE.FRAMEWORK == framework
                    && ((v.STATE == FsTemplateVersionState.Active && v.EFFECTIVE_FROM_YEAR <= year)
                        || (useDrafts && v.STATE == FsTemplateVersionState.Draft)))
                .ToListAsync(cancellationToken))
            .Select(v => (Version: v, Priority: scope.PriorityOf(v.TEMPLATE.VAHEDCODE)))
            .Where(x => x.Priority is not null)
            .ToList();

        // هر کد قالب: اول نزدیک‌ترین مالک (خود ← اجداد ← مشترک)، بعد در همان قالب پیش‌نویس (اگر
        // مجاز) و سپس فعالِ با بزرگ‌ترین سال شروع.
        var chosen = candidates
            .GroupBy(x => x.Version.TEMPLATE.CODE, StringComparer.Ordinal)
            .Select(g => g
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Version.STATE == FsTemplateVersionState.Draft ? 0 : 1)
                .ThenByDescending(x => x.Version.EFFECTIVE_FROM_YEAR)
                .First().Version)
            .OrderBy(v => v.TEMPLATE.ORDER_NO)
            .ThenBy(v => v.TEMPLATE.CODE, StringComparer.Ordinal)
            .ToList();

        var result = new List<FsTemplateVersionDetailDto>(chosen.Count);

        foreach (var v in chosen)
        {
            result.Add(await ToDetailAsync(v, false, cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlySet<string>> GetTemplateCodesAsync(CancellationToken cancellationToken = default)
    {
        var codes = await _dbContext.TB_FS_TEMPLATEs
            .AsNoTracking()
            .Where(t => !t.ISDELETED)
            .Select(t => t.CODE)
            .ToListAsync(cancellationToken);

        return codes.ToHashSet(StringComparer.Ordinal);
    }

    private async Task<FsTemplateVersionDetailDto> ToDetailAsync(TB_FS_TEMPLATE_VERSION version, bool canEdit, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.TB_FS_TEMPLATE_ROWs
            .AsNoTracking()
            .Where(r => r.VERSION_ID == version.ID)
            .OrderBy(r => r.ORDER_NO)
            .ThenBy(r => r.CODE)
            .ToListAsync(cancellationToken);

        var codeById = rows.ToDictionary(r => r.ID, r => r.CODE);

        return new FsTemplateVersionDetailDto(
            version.ID,
            version.TEMPLATE_ID,
            version.TEMPLATE.VAHEDCODE,
            canEdit,
            version.TEMPLATE.CODE,
            version.TEMPLATE.TITLE_FA,
            version.TEMPLATE.FRAMEWORK,
            version.TEMPLATE.STATEMENT_TYPE,
            version.TEMPLATE.NOTE_PARENT_TEMPLATE_CODE,
            version.TEMPLATE.NOTE_PARENT_ROW_CODE,
            version.TEMPLATE.NOTE_TOTAL_ROW_CODE,
            version.VERSION_NO,
            version.STATE,
            version.EFFECTIVE_FROM_YEAR,
            version.DESCRIPTION,
            version.ACTIVATED_BY,
            version.ACTIVATED_DATE,
            version.CONTENT_HASH,
            rows.Select(r => new FsTemplateRowDto(
                r.ID,
                r.CODE,
                r.PARENT_ID,
                r.PARENT_ID is { } pid ? codeById.GetValueOrDefault(pid) : null,
                r.ORDER_NO,
                r.ROW_TYPE,
                r.TITLE_FA,
                r.TITLE_EN,
                r.NOTE_REF,
                r.NORMAL_BALANCE,
                r.SELECTOR,
                r.VALUE_TYPE,
                r.FORMULA,
                FsRowFormat.FromJson(r.FORMAT_JSON),
                r.IS_DRILLABLE,
                r.ALLOW_MANUAL_ADJUST)).ToList());
    }
}
