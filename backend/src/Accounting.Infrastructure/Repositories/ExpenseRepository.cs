using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IExpenseRepository"/>. Only stages the entity
/// via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class ExpenseRepository : IExpenseRepository
{
    private readonly LegacyDbContext _dbContext;

    public ExpenseRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_EXPENCE expense, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_EXPENCEs.AddAsync(expense, cancellationToken);
    }

    public async Task<TB_EXPENCE?> GetForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_EXPENCEs
            .FirstOrDefaultAsync(e => e.ID == id, cancellationToken);

        // Fetched by ID alone, then judged — a WHERE on VAHEDCODE could not tell "no such row"
        // apart from "another unit's row", and those answer 404 and 403 respectively.
        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "Expense");

        return entity;
    }

    public async Task AddTafsiliLinkAsync(TB_EXPENCE_LINK_TAFSILI link, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_EXPENCE_LINK_TAFSILIs.AddAsync(link, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_EXPENCE_LINK_TAFSILI>> GetActiveTafsiliLinksAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        // Change-tracked on purpose (no AsNoTracking): the update handler soft-deletes dropped
        // links by mutating these instances in place. ISDELETED is non-nullable here, so
        // "== false" is the complete "active" predicate.
        return await _dbContext.TB_EXPENCE_LINK_TAFSILIs
            .Where(l => l.EXPENSE_ID == expenseId && l.ISDELETED == false)
            .ToListAsync(cancellationToken);
    }
}
