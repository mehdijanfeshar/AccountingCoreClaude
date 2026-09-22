using System.Linq.Expressions;
using Accounting.Application.IdentitySubGroups.Queries;
using Accounting.Application.Common;
using Accounting.Domain.ValueObjects;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IIdentitySubGroupReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class IdentitySubGroupReadRepository : IIdentitySubGroupReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_IDENTITYSUBGRP, IdentitySubGroupDto>> ToDto = s => new IdentitySubGroupDto(
        s.ID,
        s.IDENTYGROUPS_ID,
        s.SUBGRPS_DESC,
        s.SUBGRPS_LEN,
        s.SUMFLAG,
        s.FIXED,
        s.SUBGRPS_TYPE,
        s.VAHEDCODE,
        s.YEAR,
        s.IDENTYSUBGROUPS_CODE,
        s.CREATEDDATE,
        s.UPDATEDDATE,
        s.ADDUSERID,
        s.CHANGEUSERID,
        s.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public IdentitySubGroupReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<IdentitySubGroupDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        Guid? identityGroupId = null,
        IdentitySubGroupKind? kind = null,
        CancellationToken cancellationToken = default)
    {
        // VahedCode filter: deliberately unconditional — no "if (!string.IsNullOrEmpty(vahedCode))"
        // guard. That exact conditional pattern is precisely the IDOR hole CLAUDE.md risk #1
        // describes: it lets a caller with no usable unit scope see every unit's rows instead of
        // none. VahedScopeBehavior guarantees vahedCode is always a real, non-empty value here,
        // so no such guard is needed — and adding one back would silently reopen the hole for any
        // future caller path that manages to reach this method with an empty string.
        //
        // Also deliberately exact-equality only: rows are matched with exact equality against
        // VAHEDCODE (non-nullable string on this table).
        var query = _dbContext.TB_IDENTITYSUBGRPs
            .AsNoTracking()
            .Where(s => s.ISDELETED != true && s.VAHEDCODE == vahedCode);

        // Both filters are opt-in: null means "do not narrow", never "match nothing". Note this
        // is the opposite of the VAHEDCODE guard above — a narrowing filter is safe to skip, a
        // security scope is not.
        //
        // Together these two are what the شناسنامه entry form needs: "give me the FIXED
        // subgroups of group X", which is the reference app's getFixed?Groupid= call. Without
        // them the form would have to page through every subgroup of the unit and filter client
        // side.
        if (identityGroupId is not null)
        {
            query = query.Where(s => s.IDENTYGROUPS_ID == identityGroupId);
        }

        if (kind is not null)
        {
            query = query.Where(s => s.FIXED == kind);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // No stable natural business key exists that is guaranteed non-null on its own
            // (IDENTYSUBGROUPS_CODE is optional), so rows are ordered by creation time with ID
            // as a pure tie-breaker for stable paging.
            .OrderBy(s => s.CREATEDDATE)
            .ThenBy(s => s.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<IdentitySubGroupDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<IdentitySubGroupDto?> GetByIdAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // The owning unit is read first as a scalar so the access decision can tell "no such
        // row" (null, caller gets null, 404) from "another unit's row" (403). Projecting it
        // alongside the DTO in one query is not possible while ToDto stays a reusable
        // Expression, and this is a single-record edit-form fetch, not a hot path.
        var ownerVahedCode = await _dbContext.TB_IDENTITYSUBGRPs
            .AsNoTracking()
            .Where(s => s.ID == id)
            .Select(s => s.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "IdentitySubGroup");

        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on IdentitySubGroupDto.IsDeleted.
        return await _dbContext.TB_IDENTITYSUBGRPs
            .AsNoTracking()
            .Where(s => s.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
