using Accounting.Application.Common.Interfaces;
using Accounting.Application.UnitAccess.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IUnitAccessReadRepository"/> — the org-unit tree
/// walk behind «دسترسی به زیرمجموعه».
/// </summary>
public sealed class UnitAccessReadRepository : IUnitAccessReadRepository
{
    /// <summary>
    /// <c>TB_VAHED_TYPE.TYPECODE</c> that grants blanket access to every unit.
    ///
    /// <para>
    /// Ported verbatim from the reference project, where the value is an unexplained literal in
    /// <c>VahedInfoRepository.GetAllVahedInfoByParentAsync</c>:
    /// <c>if (currentVahed == "17") return _dbSet.AsNoTracking();</c>. In that same method the
    /// codes <c>"10"</c>–<c>"14"</c> sit commented out immediately above it, so other
    /// headquarters-ish types were evidently once global too and were narrowed to just this one.
    /// </para>
    ///
    /// <para>
    /// Confirmed as ستاد مرکزی by the project owner (2026-09-22), and consistent with the
    /// phase-19 finding that a real Tamin IDP token for a ستاد مرکزی user carries org claim
    /// <c>"0000"</c>. ⚠️ The mapping "TYPECODE 17 == ستاد" itself has <b>not</b> been verified
    /// against live Oracle data — see docs/open-decisions.md, phase 37.
    /// </para>
    /// </summary>
    public const string HeadquartersVahedTypeCode = "17";

    private readonly LegacyDbContext _dbContext;

    public UnitAccessReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AccessibleUnitDto>> GetAccessibleUnitsAsync(
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vahedCode))
        {
            return Array.Empty<AccessibleUnitDto>();
        }

        // TB_VAHED_INFO is a small lookup (organizational units, not transactional data), and the
        // subtree walk below is inherently recursive. Pulling the flat set once and traversing in
        // memory is both simpler and cheaper than either an Oracle recursive CTE expressed through
        // raw SQL or a per-level round trip (which would be a textbook N+1 over tree depth).
        var units = await _dbContext.TB_VAHED_INFOs
            .AsNoTracking()
            .Select(v => new UnitRow(v.ID, v.VAHEDCODE, v.VAHEDNAME, v.PARENT_ID, v.VAHEDTYPE.TYPECODE))
            .ToListAsync(cancellationToken);

        var self = units.FirstOrDefault(u => string.Equals(u.VahedCode, vahedCode, StringComparison.Ordinal));

        // Fail closed. A token claim that matches no Legacy row is a misconfiguration, and the one
        // thing it must never do is fall through to "then everything is fine".
        if (self is null)
        {
            return Array.Empty<AccessibleUnitDto>();
        }

        var reachable = string.Equals(self.VahedTypeCode, HeadquartersVahedTypeCode, StringComparison.Ordinal)
            ? units
            : CollectSubtree(units, self);

        return reachable
            .OrderBy(u => u.VahedCode, StringComparer.Ordinal)
            .Select(u => new AccessibleUnitDto(
                u.Id,
                u.VahedCode,
                u.VahedName,
                u.ParentId,
                IsDefault: u.Id == self.Id))
            .ToList();
    }

    public async Task<UnitProfile?> GetUnitProfileAsync(
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vahedCode))
        {
            return null;
        }

        // Same Oracle constraint as CanActAsAsync: select the raw TYPECODE, decide in C#. A
        // projected boolean comparison becomes a TRUE/FALSE literal and fails with ORA-00904.
        var row = await _dbContext.TB_VAHED_INFOs
            .AsNoTracking()
            .Where(v => v.VAHEDCODE == vahedCode)
            .Select(v => new { v.VAHEDNAME, TypeCode = v.VAHEDTYPE.TYPECODE })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new UnitProfile(
                row.VAHEDNAME,
                string.Equals(row.TypeCode, HeadquartersVahedTypeCode, StringComparison.Ordinal));
    }

    /// <summary>
    /// Ceiling on the ancestor walk in <see cref="CanActAsAsync"/>. Real data is two levels deep
    /// (verified on live Oracle, 2026-09-22: 73 roots + 973 children, no depth 3), so this is
    /// generous. It exists because <c>PARENT_ID</c> has no FK (open risk #9) and a cycle would
    /// otherwise spin this loop forever — the same reason the descent has a visited set.
    /// </summary>
    private const int MaxAncestorWalk = 32;

    public async Task<bool> CanActAsAsync(
        string ownVahedCode,
        string targetVahedCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownVahedCode) || string.IsNullOrWhiteSpace(targetVahedCode))
        {
            return false;
        }

        // Acting as yourself needs no tree walk and no headquarters lookup. This is also the
        // overwhelmingly common case, so it stays a single indexed read.
        if (string.Equals(ownVahedCode, targetVahedCode, StringComparison.Ordinal))
        {
            return await _dbContext.TB_VAHED_INFOs
                .AsNoTracking()
                .AnyAsync(v => v.VAHEDCODE == ownVahedCode, cancellationToken);
        }

        // ⚠️ Projects the raw TYPECODE and compares in memory, rather than the more natural
        // `IsHeadquarters = v.VAHEDTYPE.TYPECODE == HeadquartersVahedTypeCode`. Oracle has no
        // boolean type: the provider renders such a projection with TRUE/FALSE literals and the
        // server answers `ORA-00904: "FALSE": invalid identifier`. Never project a boolean
        // expression in a query against this database — select the value and decide in C#.
        var own = await _dbContext.TB_VAHED_INFOs
            .AsNoTracking()
            .Where(v => v.VAHEDCODE == ownVahedCode)
            .Select(v => new { v.ID, TypeCode = v.VAHEDTYPE.TYPECODE })
            .FirstOrDefaultAsync(cancellationToken);

        if (own is null)
        {
            return false;
        }

        var ownIsHeadquarters = string.Equals(own.TypeCode, HeadquartersVahedTypeCode, StringComparison.Ordinal);

        var target = await _dbContext.TB_VAHED_INFOs
            .AsNoTracking()
            .Where(v => v.VAHEDCODE == targetVahedCode)
            .Select(v => new { v.ID, v.PARENT_ID })
            .FirstOrDefaultAsync(cancellationToken);

        // The target must exist even for headquarters. Otherwise a typo'd unit code would be
        // accepted as an effective scope for the one role that can reach everything, and every
        // downstream query would silently match zero rows instead of failing.
        if (target is null)
        {
            return false;
        }

        if (ownIsHeadquarters)
        {
            return true;
        }

        // Walk UP from the target looking for the caller, rather than down from the caller. Both
        // are correct; this direction costs O(depth) single-row lookups instead of materialising
        // the caller's entire subtree.
        var visited = new HashSet<Guid> { target.ID };
        var parentId = target.PARENT_ID;

        for (var step = 0; step < MaxAncestorWalk && parentId is not null; step++)
        {
            if (parentId.Value == own.ID)
            {
                return true;
            }

            if (!visited.Add(parentId.Value))
            {
                // A cycle. Not reachable through a legitimate hierarchy, so deny.
                return false;
            }

            var ancestorId = parentId.Value;
            parentId = await _dbContext.TB_VAHED_INFOs
                .AsNoTracking()
                .Where(v => v.ID == ancestorId)
                .Select(v => v.PARENT_ID)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Breadth-first descent from <paramref name="root"/> over <c>PARENT_ID</c>, returning the
    /// root itself plus every descendant.
    ///
    /// <para>
    /// ⚠️ <b>The visited set is load-bearing, not defensive boilerplate.</b>
    /// <c>TB_VAHED_INFO.PARENT_ID</c> carries <b>no FK constraint</b> (recorded open risk #9), so
    /// nothing in the database prevents a row from pointing at itself or two rows from pointing at
    /// each other. Without this guard such a row would spin this loop forever and hang every
    /// request that resolves unit access.
    /// </para>
    /// </summary>
    private static List<UnitRow> CollectSubtree(List<UnitRow> units, UnitRow root)
    {
        var childrenByParent = units
            .Where(u => u.ParentId is not null)
            .GroupBy(u => u.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<UnitRow>();
        var visited = new HashSet<Guid> { root.Id };
        var queue = new Queue<UnitRow>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            if (!childrenByParent.TryGetValue(current.Id, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                if (visited.Add(child.Id))
                {
                    queue.Enqueue(child);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Flat in-memory row for the traversal. Deliberately not <see cref="AccessibleUnitDto"/>:
    /// that type carries <c>IsDefault</c>, which is only knowable once the caller is known, and
    /// this one carries <c>VahedTypeCode</c>, which is an access input rather than something the
    /// client should receive.
    /// </summary>
    private sealed record UnitRow(
        Guid Id,
        string VahedCode,
        string VahedName,
        Guid? ParentId,
        string? VahedTypeCode);
}
