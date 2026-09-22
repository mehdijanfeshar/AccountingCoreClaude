using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAccountCodeRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never
/// calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class AccountCodeRepository : IAccountCodeRepository
{
    private readonly LegacyDbContext _dbContext;

    public AccountCodeRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_ACCOUNTCODE accountCode, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ACCOUNTCODEs.AddAsync(accountCode, cancellationToken);
    }

    public async Task<TB_ACCOUNTCODE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_ACCOUNTCODEs
            .FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
    }

    public async Task<TB_ACCOUNT_LINK_TAFSILGROUP?> GetTafsilGroupLinkForUpdateAsync(
        Guid accountCodeId,
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_ACCOUNT_LINK_TAFSILGROUPs
            .FirstOrDefaultAsync(l => l.ID == linkId && l.ACCOUNT_ID == accountCodeId, cancellationToken);
    }

    public async Task AddTafsilGroupLinkAsync(
        TB_ACCOUNT_LINK_TAFSILGROUP link,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ACCOUNT_LINK_TAFSILGROUPs.AddAsync(link, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_ACCOUNT_LINK_TAFSILGROUP>> GetTafsilGroupLinksForSyncAsync(
        Guid accountCodeId,
        CancellationToken cancellationToken = default)
    {
        var stored = await _dbContext.TB_ACCOUNT_LINK_TAFSILGROUPs
            .Where(l => l.ACCOUNT_ID == accountCodeId)
            .ToListAsync(cancellationToken);

        // A row staged by AddTafsilGroupLinkAsync in this same unit of work is not in the database
        // yet, so the query above cannot see it — EF sends queries to the server, it does not
        // replay them against pending inserts. Merging them here is what makes the caller's own
        // change visible to the level synchronizer; without it, creating the first link for a
        // level would compute "no links for this level" and retire the very level it just enabled.
        var pendingInserts = _dbContext.ChangeTracker
            .Entries<TB_ACCOUNT_LINK_TAFSILGROUP>()
            .Where(entry => entry.State == EntityState.Added && entry.Entity.ACCOUNT_ID == accountCodeId)
            .Select(entry => entry.Entity)
            .Where(entity => !stored.Contains(entity));

        stored.AddRange(pendingInserts);

        return stored;
    }

    public async Task<IReadOnlyList<TB_ACCOUNT_LINK_LEVEL>> GetLevelLinksForSyncAsync(
        Guid accountCodeId,
        CancellationToken cancellationToken = default)
    {
        // Change-tracked and unfiltered on purpose: the synchronizer both revives soft-deleted
        // rows and retires live ones, so it needs every row for this معین as a mutable entity.
        return await _dbContext.TB_ACCOUNT_LINK_LEVELs
            .Where(l => l.ACCOUNT_ID == accountCodeId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddLevelLinksAsync(
        IReadOnlyCollection<TB_ACCOUNT_LINK_LEVEL> links,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ACCOUNT_LINK_LEVELs.AddRangeAsync(links, cancellationToken);
    }
}
