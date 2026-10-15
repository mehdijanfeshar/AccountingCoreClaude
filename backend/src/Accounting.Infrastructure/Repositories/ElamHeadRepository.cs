using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IElamHeadRepository"/>. Only stages the entity
/// via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
///
/// ⚠️ HEAD ONLY — no method here touches <c>TB_ELAMDETAIL</c>.
/// </summary>
public sealed class ElamHeadRepository : IElamHeadRepository
{
    private readonly LegacyDbContext _dbContext;

    public ElamHeadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_ELAMHEAD elamHead, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ELAMHEADs.AddAsync(elamHead, cancellationToken);
    }

    public async Task<TB_ELAMHEAD?> GetForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_ELAMHEADs
            .FirstOrDefaultAsync(e => e.ID == id, cancellationToken);

        // Fetched by ID alone, then judged — a WHERE on VAHEDCODE could not tell "no such row"
        // apart from "another unit's row", and those answer 404 and 403 respectively.
        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "ElamHead");

        return entity;
    }

    public async Task SoftDeleteDetailTreeAsync(
        Guid headId,
        string? changeUserId,
        DateTime updatedDate,
        CancellationToken cancellationToken = default)
    {
        var details = await _dbContext.TB_ELAMDETAILs
            .Where(d => d.ELAMHEAD_ID == headId)
            .ToListAsync(cancellationToken);

        foreach (var detail in details.Where(d => d.ISDELETED != true))
        {
            detail.ISDELETED = true;
            detail.CHANGEUSERID = changeUserId;
            detail.UPDATEDDATE = updatedDate;
        }

        var detailIds = details.Select(d => d.ID).ToList();

        var links = await _dbContext.TB_ELAMDETAIL_LINK_TAFSILIs
            .Where(l => detailIds.Contains(l.ELAMDETAIL_ID!.Value) && l.ISDELETED != true)
            .ToListAsync(cancellationToken);

        foreach (var link in links)
        {
            link.ISDELETED = true;
            link.CHANGEUSERID = changeUserId;
            link.UPDATEDDATE = updatedDate;
        }
    }
}
