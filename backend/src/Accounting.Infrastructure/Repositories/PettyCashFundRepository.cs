using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPettyCashFundRepository"/>. Only stages changes
/// via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class PettyCashFundRepository : IPettyCashFundRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashFundRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PC_FUND fund, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PC_FUNDs.AddAsync(fund, cancellationToken);
    }

    public async Task<TB_PC_FUND?> GetForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_PC_FUNDs
            .FirstOrDefaultAsync(f => f.ID == id, cancellationToken);

        // Fetched by ID alone, then judged — a WHERE on VAHEDCODE could not tell "no such row"
        // apart from "another unit's row", and those answer 404 and 403 respectively.
        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PettyCashFund");

        return entity;
    }

    public async Task<bool> ExistsByCodeAsync(
        string code,
        string vahedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_PC_FUNDs
            .AsNoTracking()
            .Where(f => !f.ISDELETED && f.VAHEDCODE == vahedCode && f.CODE == code);

        if (excludeId is { } id)
        {
            query = query.Where(f => f.ID != id);
        }

        // CountAsync, not AnyAsync: the Oracle provider renders AnyAsync as
        // "CASE WHEN EXISTS ... THEN True ELSE False", which pre-23ai Oracle rejects (ORA-00904).
        return await query.CountAsync(cancellationToken) > 0;
    }

    public async Task<bool> HasActiveExpenseDocsAsync(Guid fundId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_PC_EXPENSE_DOCs
            .AsNoTracking()
            .Where(d => d.FUND_ID == fundId && !d.ISDELETED)
            .CountAsync(cancellationToken) > 0;
    }
}
