using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;
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
                    r.AMOUNT_PRV)).ToList())).ToList());
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
        r.CREATEDDATE);
}
