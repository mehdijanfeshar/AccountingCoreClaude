using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IChargeAndCostRepository"/>. Only stages
/// changes — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class ChargeAndCostRepository : IChargeAndCostRepository
{
    private readonly LegacyDbContext _dbContext;

    public ChargeAndCostRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddHeadAsync(TB_CHARGEANDCOST_HEAD head, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_CHARGEANDCOST_HEADs.AddAsync(head, cancellationToken);
    }

    public async Task AddDetailAsync(TB_CHARGEANDCOST_DETAIL detail, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_CHARGEANDCOST_DETAILs.AddAsync(detail, cancellationToken);
    }

    public async Task<TB_CHARGEANDCOST_HEAD?> GetHeadForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var head = await _dbContext.TB_CHARGEANDCOST_HEADs
            .FirstOrDefaultAsync(h => h.ID == id, cancellationToken);

        // Fetched by ID alone, then judged — mirrors every other by-id repository method in this
        // project (see VahedOwnership XML doc).
        VahedOwnership.EnsureOwned(head?.VAHEDCODE, vahedCode, id, "ChargeAndCostHead");

        return head;
    }

    public async Task<TB_CHARGEANDCOST_DETAIL?> GetSingleDetailForUpdateAsync(
        Guid headId,
        CancellationToken cancellationToken = default)
    {
        // By design there is never more than one detail row per head for the petty-cash module
        // (docs/centralaccount-business-reference.md §24-5-4) — FirstOrDefault, not Single, so a
        // pre-existing live row that happens to violate that (outside this module's control)
        // does not throw.
        return await _dbContext.TB_CHARGEANDCOST_DETAILs
            .FirstOrDefaultAsync(d => d.CHARGEANDCOSTHEAD_ID == headId, cancellationToken);
    }

    public async Task<int> GetNextCodeAsync(
        string vahedCode,
        string year,
        ChargeAndCostType type,
        CancellationToken cancellationToken = default)
    {
        // Deleted heads are counted on purpose — mirrors IdentityHeadRepository.GetNextSerialAsync:
        // the real UK_CHARGEANDCOST-style uniqueness (docs/centralaccount-business-reference.md
        // §24-5-4: "کنترل تکرار روی (ChargeAndCostCode, VahedCode, Year)") is enforced regardless
        // of ISDELETED, so skipping a soft-deleted row's code would hand out a value that then
        // collides. Parsed client-side rather than with a SQL MAX, mirroring
        // VoucherHeadRepository.GetNextDocNumAsync: CHARGEANDCOST_CODE is a varchar and a
        // lexicographic MAX would not sort numerically for values of different width.
        var existingCodes = await _dbContext.TB_CHARGEANDCOST_HEADs
            .AsNoTracking()
            .Where(h => h.VAHEDCODE == vahedCode && h.YEAR == year && h.CHARGEANDCOST_TYPE == type)
            .Select(h => h.CHARGEANDCOST_CODE)
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

    public async Task AddLinkAsync(TB_CHARGE_LINK_COST link, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_CHARGE_LINK_COSTs.AddAsync(link, cancellationToken);
    }

    public async Task<bool> ExistsActiveLinkForCostAsync(Guid costId, CancellationToken cancellationToken = default)
    {
        // CountAsync, not AnyAsync — same Oracle-provider reasoning as
        // PettyCashExpenseDocRepository.ExistsActiveDuplicateAsync.
        return await _dbContext.TB_CHARGE_LINK_COSTs
            .AsNoTracking()
            .Where(l => !l.ISDELETED && l.COST_ID == costId)
            .CountAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<TB_CHARGE_LINK_COST>> GetActiveLinksByChargeIdAsync(
        Guid chargeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_CHARGE_LINK_COSTs
            .Where(l => !l.ISDELETED && l.CHARGE_ID == chargeId)
            .ToListAsync(cancellationToken);
    }
}
