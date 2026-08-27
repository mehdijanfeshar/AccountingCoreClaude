using System.Linq.Expressions;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PersonActions.Queries;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPersonActionReadRepository"/>. Reads directly
/// from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> — same narrow exception to the
/// "reports read from a Read Model" rule as <c>AccountCodeReadRepository</c>.
/// </summary>
public sealed class PersonActionReadRepository : IPersonActionReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/>, EF Core translates it into a column-level SQL projection.
    /// </summary>
    private static readonly Expression<Func<TB_PERSON_ACTION, PersonActionDto>> ToDto = p => new PersonActionDto(
        p.ID,
        p.USERNAME,
        p.USERID,
        p.FROMDATE,
        p.TODATE,
        p.STATUS,
        p.OPERATORROLE,
        p.VAHEDCODE,
        p.CREATEDDATE,
        p.UPDATEDDATE,
        p.ADDUSERID,
        p.CHANGEUSERID,
        p.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public PersonActionReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PersonActionDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_PERSON_ACTIONs
            .AsNoTracking()
            .Where(p => p.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // USERID participates in UK_PERSON_ACTION but is not itself unique, so rows are
            // ordered by USERID first (alphabetical, string column) with ID as a pure
            // tie-breaker for stable paging.
            .OrderBy(p => p.USERID)
            .ThenBy(p => p.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<PersonActionDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<PersonActionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_PERSON_ACTIONs
            .AsNoTracking()
            .Where(p => p.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
