using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class FsRunRepository : IFsRunRepository
{
    private readonly LegacyDbContext _dbContext;

    public FsRunRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_FS_RUN run, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_RUNs.AddAsync(run, cancellationToken);
    }

    public async Task<int> GetNextRunNoAsync(CancellationToken cancellationToken = default)
    {
        var max = await _dbContext.TB_FS_RUNs.Select(r => (int?)r.RUN_NO).MaxAsync(cancellationToken);
        return (max ?? 0) + 1;
    }

    public Task<TB_FS_RUN?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_FS_RUNs
            .FirstOrDefaultAsync(r => r.ID == id && r.VAHEDCODE == vahedCode && !r.ISDELETED, cancellationToken);
    }

    public async Task<IReadOnlyList<FsRunSummaryDto>> ListAsync(string vahedCode, string? year, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_FS_RUNs.AsNoTracking().Where(r => r.VAHEDCODE == vahedCode && !r.ISDELETED);

        if (!string.IsNullOrEmpty(year))
        {
            query = query.Where(r => r.YEAR == year);
        }

        var runs = await query
            .OrderByDescending(r => r.RUN_NO)
            .Take(200)
            .Select(r => new { Run = r, StatementCount = r.TB_FS_RUN_STATEMENTs.Count })
            .ToListAsync(cancellationToken);

        return runs.Select(x => ToSummary(x.Run, x.StatementCount)).ToList();
    }

    public async Task<FsRunDetailDto?> GetDetailAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.TB_FS_RUNs
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ID == id && r.VAHEDCODE == vahedCode && !r.ISDELETED, cancellationToken);

        if (run is null)
        {
            return null;
        }

        var statements = await _dbContext.TB_FS_RUN_STATEMENTs
            .AsNoTracking()
            .Where(s => s.RUN_ID == id && s.VAHEDCODE == vahedCode)
            .OrderBy(s => s.ORDER_NO)
            .ThenBy(s => s.TEMPLATE_CODE)
            .ToListAsync(cancellationToken);

        var statementIds = statements.Select(s => s.ID).ToList();

        var rows = await _dbContext.TB_FS_RUN_ROWs
            .AsNoTracking()
            .Where(r => r.RUN_ID == id && r.VAHEDCODE == vahedCode && statementIds.Contains(r.RUN_STATEMENT_ID))
            .OrderBy(r => r.ORDER_NO)
            .ToListAsync(cancellationToken);

        var rowsByStatement = rows.ToLookup(r => r.RUN_STATEMENT_ID);

        var checks = await _dbContext.TB_FS_RUN_CHECKs.AsNoTracking()
            .Where(c => c.RUN_ID == id && c.VAHEDCODE == vahedCode)
            .ToListAsync(cancellationToken);
        var actions = await _dbContext.TB_FS_RUN_ACTIONs.AsNoTracking()
            .Where(a => a.RUN_ID == id && a.VAHEDCODE == vahedCode)
            .OrderBy(a => a.CREATEDDATE)
            .ToListAsync(cancellationToken);
        var manuals = await _dbContext.TB_FS_RUN_MANUALs.AsNoTracking()
            .Where(m => m.RUN_ID == id && m.VAHEDCODE == vahedCode)
            .ToListAsync(cancellationToken);

        return new FsRunDetailDto(
            ToSummary(run, statements.Count),
            run.FROM_DATE,
            run.TO_DATE,
            run.CONTENT_HASH,
            run.NOTE_START_NO,
            statements.Select(s => new FsRunStatementDto(
                s.ID,
                s.TEMPLATE_ID,
                s.VERSION_ID,
                s.TEMPLATE_CODE,
                s.TITLE_FA,
                s.STATEMENT_TYPE,
                s.ORDER_NO,
                s.VERSION_NO,
                s.VERSION_STATE,
                s.IS_NOTE,
                s.NOTE_NO,
                s.PARENT_TEMPLATE_CODE,
                s.PARENT_ROW_CODE,
                s.TOTAL_ROW_CODE,
                s.CHECK_DIFF_CUR,
                s.CHECK_DIFF_PRV,
                rowsByStatement[s.ID].Select(r => new FsRunRowDto(
                    r.ID,
                    r.ROW_CODE,
                    r.PARENT_CODE,
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
                    r.AMOUNT_CUR,
                    r.AMOUNT_PRV)).ToList())).ToList(),
            run.SOURCE_RUN_ID,
            checks
                .OrderByDescending(c => c.SEVERITY)
                .ThenBy(c => c.PASSED)
                .ThenBy(c => c.CODE)
                .Select(c => new FsRunCheckDto(
                    c.CODE, c.TITLE_FA, c.SEVERITY, c.PASSED, c.MESSAGE, c.DIFFERENCE, c.ROW_REF,
                    c.ID, c.ASSIGNEE_USERID, c.ASSIGNEE_NAME, c.DUE_DATE, c.ASSIGN_STATE, c.ASSIGNED_BY))
                .ToList(),
            actions.Select(a => new FsRunActionDto(a.ACTION, a.FROM_STATE, a.TO_STATE, a.USERID, a.COMMENTS, a.CREATEDDATE, a.STEP_NO)).ToList(),
            manuals.Select(m => new FsRunManualDto(m.TEMPLATE_CODE, m.ROW_CODE, m.AMOUNT_CUR, m.AMOUNT_PRV, m.REASON, m.ADDUSERID, m.CREATEDDATE)).ToList());
    }

    public async Task AddActionAsync(TB_FS_RUN_ACTION action, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_RUN_ACTIONs.AddAsync(action, cancellationToken);
    }

    public async Task<IReadOnlyList<FsUnitRunStatusDto>> GetLatestRunsAsync(
        IReadOnlyCollection<string> vahedCodes, string year, CancellationToken cancellationToken = default)
    {
        if (vahedCodes.Count == 0)
        {
            return [];
        }

        var runs = await _dbContext.TB_FS_RUNs
            .AsNoTracking()
            .Where(r => vahedCodes.Contains(r.VAHEDCODE) && r.YEAR == year && !r.ISDELETED && r.STATE != FsRunState.Superseded)
            .Select(r => new { r.ID, r.VAHEDCODE, r.RUN_NO, r.FRAMEWORK, r.STATE, r.CREATEDDATE })
            .ToListAsync(cancellationToken);

        var latest = runs.GroupBy(r => r.VAHEDCODE).Select(g => g.OrderByDescending(r => r.CREATEDDATE).First()).ToList();
        var ids = latest.Select(r => r.ID).ToList();

        // PASSED در حافظه — عبارت بولی در کوئری اوراکل نساز (ORA-00904).
        var checks = await _dbContext.TB_FS_RUN_CHECKs
            .AsNoTracking()
            .Where(c => ids.Contains(c.RUN_ID) && c.SEVERITY == FsCheckSeverity.Blocking)
            .Select(c => new { c.RUN_ID, c.PASSED })
            .ToListAsync(cancellationToken);
        var failed = checks.Where(c => !c.PASSED).GroupBy(c => c.RUN_ID).ToDictionary(g => g.Key, g => g.Count());

        return latest
            .Select(r => new FsUnitRunStatusDto(r.VAHEDCODE, r.ID, r.RUN_NO, r.FRAMEWORK, r.STATE, r.CREATEDDATE, failed.GetValueOrDefault(r.ID)))
            .ToList();
    }

    public async Task<IReadOnlyList<FsAssignedCheckDto>> GetOpenAssignmentsAsync(string vahedCode, string userId, CancellationToken cancellationToken = default)
    {
        var rows = await (
                from c in _dbContext.TB_FS_RUN_CHECKs.AsNoTracking()
                join r in _dbContext.TB_FS_RUNs.AsNoTracking() on c.RUN_ID equals r.ID
                where c.VAHEDCODE == vahedCode && c.ASSIGNEE_USERID == userId && c.ASSIGN_STATE == FsCheckAssignState.Open && !r.ISDELETED
                select new { r.ID, r.RUN_NO, CheckId = c.ID, c.CODE, c.TITLE_FA, c.DUE_DATE, c.ASSIGNED_BY })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new FsAssignedCheckDto(x.ID, x.RUN_NO, x.CheckId, x.CODE, x.TITLE_FA, x.DUE_DATE, x.ASSIGNED_BY)).ToList();
    }

    public async Task AddCommentAsync(TB_FS_RUN_COMMENT comment, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_RUN_COMMENTs.AddAsync(comment, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_FS_RUN_COMMENT>> GetCommentsAsync(Guid runId, string vahedCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_FS_RUN_COMMENTs
            .AsNoTracking()
            .Where(c => c.RUN_ID == runId && c.VAHEDCODE == vahedCode && !c.ISDELETED)
            .OrderBy(c => c.CREATEDDATE)
            .ToListAsync(cancellationToken);
    }

    public Task<TB_FS_RUN_COMMENT?> GetCommentForUpdateAsync(Guid runId, Guid commentId, string vahedCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_FS_RUN_COMMENTs
            .FirstOrDefaultAsync(c => c.ID == commentId && c.RUN_ID == runId && c.VAHEDCODE == vahedCode && !c.ISDELETED, cancellationToken);
    }

    public Task<TB_FS_RUN_CHECK?> GetCheckForUpdateAsync(Guid runId, Guid checkId, string vahedCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_FS_RUN_CHECKs
            .FirstOrDefaultAsync(c => c.ID == checkId && c.RUN_ID == runId && c.VAHEDCODE == vahedCode, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_FS_RUN>> GetPublishedForUpdateAsync(
        string vahedCode, FsFramework framework, string year, int toMonth, bool includeSubUnits, CancellationToken cancellationToken = default)
    {
        // INCLUDE_SUBUNITS در حافظه مقایسه می‌شود — عبارت بولی در کوئری اوراکل نساز (ORA-00904 «FALSE»).
        var candidates = await _dbContext.TB_FS_RUNs
            .Where(r => r.VAHEDCODE == vahedCode
                && r.FRAMEWORK == framework
                && r.YEAR == year
                && r.TO_MONTH == toMonth
                && r.STATE == FsRunState.Published
                && !r.ISDELETED)
            .ToListAsync(cancellationToken);

        return candidates.Where(r => r.INCLUDE_SUBUNITS == includeSubUnits).ToList();
    }

    public async Task<FsDrillTarget?> GetDrillTargetAsync(Guid runId, Guid rowId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.TB_FS_RUNs
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ID == runId && r.VAHEDCODE == vahedCode && !r.ISDELETED, cancellationToken);

        if (run is null)
        {
            return null;
        }

        var row = await _dbContext.TB_FS_RUN_ROWs
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ID == rowId && r.RUN_ID == runId && r.VAHEDCODE == vahedCode, cancellationToken);

        return row is null
            ? null
            : new FsDrillTarget(
                run.ID,
                run.VAHEDCODE,
                run.INCLUDE_SUBUNITS,
                run.YEAR,
                run.FROM_DATE,
                run.TO_DATE,
                run.MIN_DOCLIFE,
                run.HAS_PRIOR,
                row.ID,
                row.ROW_CODE,
                row.ROW_TYPE,
                row.VALUE_TYPE,
                row.TITLE_FA);
    }

    public async Task<IReadOnlyList<FsDrillAccountDto>> GetRowAccountsAsync(Guid runId, Guid rowId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var shares = await _dbContext.TB_FS_RUN_ACCOUNTs
            .AsNoTracking()
            .Where(a => a.RUN_ROW_ID == rowId && a.RUN_ID == runId && a.VAHEDCODE == vahedCode)
            .Select(a => new { a.ACCCODE, a.ACCNAME, a.AMOUNT_CUR, a.AMOUNT_PRV })
            .ToListAsync(cancellationToken);

        return shares
            .GroupBy(a => a.ACCCODE, StringComparer.Ordinal)
            .Select(g => new FsDrillAccountDto(
                g.Key,
                g.First().ACCNAME,
                g.Any(a => a.AMOUNT_CUR.HasValue) ? g.Sum(a => a.AMOUNT_CUR ?? 0) : null,
                g.Any(a => a.AMOUNT_PRV.HasValue) ? g.Sum(a => a.AMOUNT_PRV ?? 0) : null))
            .OrderBy(a => a.AccCode, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<FsDrillUnitDto>> GetRowUnitsAsync(
        Guid runId, Guid rowId, string vahedCode, string? accCode, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_FS_RUN_ACCOUNTs
            .AsNoTracking()
            .Where(a => a.RUN_ROW_ID == rowId && a.RUN_ID == runId && a.VAHEDCODE == vahedCode);

        if (!string.IsNullOrEmpty(accCode))
        {
            query = query.Where(a => a.ACCCODE == accCode);
        }

        var shares = await query
            .Select(a => new { a.SOURCE_VAHEDCODE, a.AMOUNT_CUR, a.AMOUNT_PRV })
            .ToListAsync(cancellationToken);

        return shares
            .GroupBy(a => a.SOURCE_VAHEDCODE, StringComparer.Ordinal)
            .Select(g => new FsDrillUnitDto(
                g.Key,
                null,
                g.Any(a => a.AMOUNT_CUR.HasValue) ? g.Sum(a => a.AMOUNT_CUR ?? 0) : null,
                g.Any(a => a.AMOUNT_PRV.HasValue) ? g.Sum(a => a.AMOUNT_PRV ?? 0) : null))
            .OrderBy(u => u.VahedCode, StringComparer.Ordinal)
            .ToList();
    }

    private static FsRunSummaryDto ToSummary(TB_FS_RUN r, int statementCount) => new(
        r.ID,
        r.RUN_NO,
        r.VAHEDCODE,
        r.VAHEDNAME,
        r.INCLUDE_SUBUNITS,
        r.UNIT_COUNT,
        r.FRAMEWORK,
        r.YEAR,
        r.TO_MONTH,
        r.MIN_DOCLIFE,
        r.HAS_PRIOR,
        r.USES_DRAFT,
        r.STATE,
        r.DESCRIPTION,
        statementCount,
        r.DURATION_MS,
        r.ADDUSERID,
        r.CREATEDDATE,
        r.PRIOR_RESTATED);
}
