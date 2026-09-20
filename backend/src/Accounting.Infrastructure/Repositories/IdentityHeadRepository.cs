using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IIdentityHeadRepository"/>. Stages changes only;
/// the transaction boundary stays in the handler via <c>IUnitOfWork.SaveChangesAsync</c>.
/// </summary>
public sealed class IdentityHeadRepository : IIdentityHeadRepository
{
    private readonly LegacyDbContext _dbContext;

    public IdentityHeadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_IDENTITYHEAD identityHead, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_IDENTITYHEADs.AddAsync(identityHead, cancellationToken);
    }

    public async Task<TB_IDENTITYHEAD?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_IDENTITYHEADs
            .FirstOrDefaultAsync(h => h.ID == id, cancellationToken);
    }

    public async Task AddFixItemAsync(TB_IDENTITYFIXITEM fixItem, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_IDENTITYFIXITEMs.AddAsync(fixItem, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_IDENTITYFIXITEM>> GetActiveFixItemsAsync(
        Guid identityHeadId,
        CancellationToken cancellationToken = default)
    {
        // Deliberately tracked (no AsNoTracking): the update path mutates the values of the items
        // that survive, so that an untouched item keeps its original CREATEDDATE/ADDUSERID
        // instead of being deleted and re-inserted.
        return await _dbContext.TB_IDENTITYFIXITEMs
            .Where(f => f.IDENTITYHEAD_ID == identityHeadId && f.ISDELETED == false)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextSerialAsync(
        Guid identityGroupId,
        string vahedCode,
        string year,
        CancellationToken cancellationToken = default)
    {
        // Deleted heads are counted on purpose. SERIAL participates in a UNIQUE constraint that
        // Oracle enforces regardless of ISDELETED, so skipping a soft-deleted row's serial would
        // hand out a value that then collides on insert. Serials are therefore monotonic and can
        // have gaps — which matches what a سریال is for.
        var highest = await _dbContext.TB_IDENTITYHEADs
            .Where(h => h.IDENTITYGROUPS_ID == identityGroupId
                && h.VAHEDCODE == vahedCode
                && h.YEAR == year)
            .Select(h => (int?)h.SERIAL)
            .MaxAsync(cancellationToken);

        return (highest ?? 0) + 1;
    }
}
