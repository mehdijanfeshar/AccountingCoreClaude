using Accounting.Application.Tafsilis.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ITafsiliReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports read
/// from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
///
/// Unlike <see cref="TafsilGroupReadRepository"/>, this cannot use a single shared
/// <c>Expression&lt;Func&lt;TB_TAFSILI, TafsiliDto&gt;&gt;</c> projection: <c>TB_TAFSILI</c> has
/// no EF navigation collection into <c>TB_TAFSIL_LINK_TAFSILGROUP</c> (that join table's
/// <c>TAFSIL_ID</c> column has no FK/navigation configured at all in <c>LegacyDbContext</c> —
/// only <c>TAFSILGROUP_ID</c> does), so the linked-group ids are fetched as a second, batched
/// query and merged in memory instead of a single SQL projection.
/// </summary>
public sealed class TafsiliReadRepository : ITafsiliReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public TafsiliReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<TafsiliDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        //
        // VahedCode filter: deliberately unconditional, exact-equality only — same rationale as
        // ExpenseReadRepository.GetPagedAsync (CLAUDE.md risk #1 / fail-closed on NULL VAHEDCODE).
        var query = _dbContext.TB_TAFSILIs
            .AsNoTracking()
            .Where(t => t.ISDELETED != true && t.VAHEDCODE == vahedCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            // TAFSILI_CODE alone enforces UK_TASILI, so it is already effectively unique among
            // non-deleted rows, but ID is kept as a tie-breaker for stable paging regardless.
            .OrderBy(t => t.TAFSILI_CODE)
            .ThenBy(t => t.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var groupLinksByTafsiliId = await GetGroupLinksByTafsiliIdAsync(
            rows.Select(r => r.ID).ToList(),
            cancellationToken);

        var items = rows
            .Select(t => ToDto(t, groupLinksByTafsiliId.GetValueOrDefault(t.ID, Array.Empty<Guid>())))
            .ToList();

        return new PagedResult<TafsiliDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<TafsiliDto?> GetByIdAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // Still no ISDELETED filter: GetById returns the row regardless of its deletion state and
        // the caller decides what to do with TafsiliDto.IsDeleted. The owning unit, however, is no
        // longer ignored — that part of this comment described the open half of IDOR risk #1.
        var entity = await _dbContext.TB_TAFSILIs
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ID == id, cancellationToken);

        // Tafsili is the entity that makes the blank-VAHEDCODE rule load-bearing: the reference
        // project writes VahedCode = "" for Owner = Global records, and those must stay visible to
        // every unit. VahedOwnership.EnsureOwned treats blank as shared rather than as denied.
        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "Tafsili");

        if (entity is null)
        {
            return null;
        }

        var groupIds = await _dbContext.TB_TAFSIL_LINK_TAFSILGROUPs
            .AsNoTracking()
            .Where(l => l.TAFSIL_ID == id && l.ISDELETED == false)
            .Select(l => l.TAFSILGROUP_ID)
            .ToListAsync(cancellationToken);

        return ToDto(entity, groupIds);
    }

    private async Task<Dictionary<Guid, IReadOnlyList<Guid>>> GetGroupLinksByTafsiliIdAsync(
        List<Guid> tafsiliIds,
        CancellationToken cancellationToken)
    {
        if (tafsiliIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<Guid>>();
        }

        var links = await _dbContext.TB_TAFSIL_LINK_TAFSILGROUPs
            .AsNoTracking()
            .Where(l => tafsiliIds.Contains(l.TAFSIL_ID) && l.ISDELETED == false)
            .Select(l => new { l.TAFSIL_ID, l.TAFSILGROUP_ID })
            .ToListAsync(cancellationToken);

        return links
            .GroupBy(l => l.TAFSIL_ID)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(x => x.TAFSILGROUP_ID).ToList());
    }

    private static TafsiliDto ToDto(TB_TAFSILI t, IReadOnlyList<Guid> tafsilGroupIds) => new(
        t.ID,
        t.TAFSILI_CODE,
        t.TAFSILI_NAME,
        t.TAFSIL_DESC,
        t.ISACTIVE,
        t.PERSONTYPE,
        t.OWNER,
        t.VAHEDTYPE,
        t.VAHEDCODE,
        tafsilGroupIds,
        t.CREATEDDATE,
        t.UPDATEDDATE,
        t.ADDUSERID,
        t.CHANGEUSERID,
        t.ISDELETED);
}
