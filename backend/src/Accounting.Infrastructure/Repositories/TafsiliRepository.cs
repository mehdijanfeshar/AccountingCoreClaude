using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ITafsiliRepository"/>. Only stages changes —
/// never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class TafsiliRepository : ITafsiliRepository
{
    private readonly LegacyDbContext _dbContext;

    public TafsiliRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TAFSILI tafsili, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TAFSILIs.AddAsync(tafsili, cancellationToken);
    }

    public async Task<TB_TAFSILI?> GetForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_TAFSILIs
            .FirstOrDefaultAsync(t => t.ID == id, cancellationToken);

        // Fetched by ID alone, then judged — a WHERE on VAHEDCODE could not tell "no such row"
        // apart from "another unit's row", and those answer 404 and 403 respectively.
        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "Tafsili");

        return entity;
    }

    public async Task<IReadOnlyList<TB_TAFSIL_LINK_TAFSILGROUP>> GetTafsiliGroupLinksAsync(
        Guid tafsiliId,
        CancellationToken cancellationToken = default)
    {
        // Change-tracked on purpose (no AsNoTracking): UpdateTafsiliCommandHandler mutates the
        // returned rows in place to soft-delete the links the caller dropped, and relies on its
        // own single SaveChangesAsync to persist them alongside the تفصیلی row itself.
        return await _dbContext.TB_TAFSIL_LINK_TAFSILGROUPs
            .Where(l => l.TAFSIL_ID == tafsiliId && l.ISDELETED == false)
            .ToListAsync(cancellationToken);
    }

    public async Task AddTafsiliGroupLinkAsync(
        TB_TAFSIL_LINK_TAFSILGROUP link,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TAFSIL_LINK_TAFSILGROUPs.AddAsync(link, cancellationToken);
    }
}
