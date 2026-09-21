using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IBankAccountRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class BankAccountRepository : IBankAccountRepository
{
    private readonly LegacyDbContext _dbContext;

    public BankAccountRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_ACCOUNT bankAccount, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ACCOUNTs.AddAsync(bankAccount, cancellationToken);
    }

    public async Task<TB_ACCOUNT?> GetForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_ACCOUNTs
            .FirstOrDefaultAsync(a => a.ID == id, cancellationToken);

        // Fetched by ID alone, then judged — a WHERE on VAHEDCODE could not tell "no such row"
        // apart from "another unit's row", and those answer 404 and 403 respectively.
        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "BankAccount");

        return entity;
    }

    public async Task AddTafsiliLinkAsync(TB_ACCOUNT_LINK_TAFSILI link, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ACCOUNT_LINK_TAFSILIs.AddAsync(link, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_ACCOUNT_LINK_TAFSILI>> GetActiveTafsiliLinksAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        // Change-tracked on purpose (no AsNoTracking): the update handler soft-deletes the
        // dropped links by mutating these instances in place. ISDELETED is a non-nullable bool
        // on this table, so "== false" is the complete "active" predicate — no NULL branch.
        return await _dbContext.TB_ACCOUNT_LINK_TAFSILIs
            .Where(l => l.ACCOUNT_ID == accountId && l.ISDELETED == false)
            .ToListAsync(cancellationToken);
    }
}
