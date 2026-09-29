using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPayReciveHeadRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
///
/// ⚠️ HEAD ONLY — no method here touches <c>TB_PAYRECIVDETAIL</c>.
/// </summary>
public sealed class PayReciveHeadRepository : IPayReciveHeadRepository
{
    private readonly LegacyDbContext _dbContext;

    public PayReciveHeadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PAYRECIVHEAD payReciveHead, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PAYRECIVHEADs.AddAsync(payReciveHead, cancellationToken);
    }

    public async Task<TB_PAYRECIVHEAD?> GetForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_PAYRECIVHEADs
            .FirstOrDefaultAsync(e => e.ID == id, cancellationToken);

        // Fetched by ID alone, then judged — a WHERE on VAHEDCODE could not tell "no such row"
        // apart from "another unit's row", and those answer 404 and 403 respectively.
        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PayReciveHead");

        return entity;
    }

    public async Task<int> GetNextCodeAsync(string vahedCode, string year, CancellationToken cancellationToken = default)
    {
        var existingCodes = await _dbContext.TB_PAYRECIVHEADs
            .AsNoTracking()
            .Where(h => h.VAHEDCODE == vahedCode && h.YEAR == year)
            .Select(h => h.PAYRECIVCODE)
            .ToListAsync(cancellationToken);

        var highest = 0;

        foreach (var code in existingCodes)
        {
            if (int.TryParse(code, out var parsed) && parsed > highest)
            {
                highest = parsed;
            }
        }

        return highest + 1;
    }

    public async Task AddDetailAsync(TB_PAYRECIVDETAIL payReciveDetail, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PAYRECIVDETAILs.AddAsync(payReciveDetail, cancellationToken);
    }

    public async Task AddDetailTafsiliLinkAsync(TB_PAYRECIVDETAIL_LINK_TAFSILI tafsiliLink, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PAYRECIVDETAIL_LINK_TAFSILIs.AddAsync(tafsiliLink, cancellationToken);
    }
}
