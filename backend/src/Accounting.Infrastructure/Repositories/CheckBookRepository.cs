using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ICheckBookRepository"/>. Only stages the entity
/// via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class CheckBookRepository : ICheckBookRepository
{
    private readonly LegacyDbContext _dbContext;

    public CheckBookRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_CHECKBOOK checkBook, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_CHECKBOOKs.AddAsync(checkBook, cancellationToken);
    }

    public async Task<TB_CHECKBOOK?> GetForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_CHECKBOOKs
            .FirstOrDefaultAsync(c => c.ID == id, cancellationToken);

        // Fetched by ID alone, then judged — a WHERE on VAHEDCODE could not tell "no such row"
        // apart from "another unit's row", and those answer 404 and 403 respectively.
        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "CheckBook");

        return entity;
    }

    public async Task<IReadOnlyList<TB_CHECK>> GetLeavesForUpdateAsync(
        Guid checkBookId, CancellationToken cancellationToken = default, bool includeDeleted = false)
        => await _dbContext.TB_CHECKs.Where(c => c.CHECKBOOK_ID == checkBookId && (includeDeleted || !c.ISDELETED))
            .ToListAsync(cancellationToken);

    public Task<int> CountLeavesInVouchersAsync(Guid checkBookId, CancellationToken cancellationToken = default)
        => (from d in _dbContext.TB_VOUCHERSDETAILs
            join c in _dbContext.TB_CHECKs on d.CHECK_ID equals c.ID
            where c.CHECKBOOK_ID == checkBookId && d.ISDELETED != true && d.VOUCHERSHEAD!.ISDELETED != true
            select d.ID).CountAsync(cancellationToken);

    public async Task<string?> GetMaxChequeNoAsync(Guid checkBookId, CancellationToken cancellationToken = default)
        => await _dbContext.TB_CHECKs.Where(c => c.CHECKBOOK_ID == checkBookId)
            .MaxAsync(c => (string?)c.CHEQ_NO, cancellationToken);

    public Task<TB_CHECKBOOK?> FindSameRangeForUpdateAsync(
        Guid accountId, string from, string to, string vahedCode, CancellationToken cancellationToken = default)
        => _dbContext.TB_CHECKBOOKs.FirstOrDefaultAsync(
            b => b.ACCOUNT_ID == accountId && b.FROMCHECKNUMBER == from && b.TOCHECKNUMBER == to && b.VAHEDCODE == vahedCode,
            cancellationToken);
}
