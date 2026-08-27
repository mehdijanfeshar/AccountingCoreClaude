using System.Linq.Expressions;
using Accounting.Application.AccountExceptions.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAccountExceptionReadRepository"/>. Reads
/// directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> — same narrow
/// exception to the "reports read from a Read Model" rule as <c>AccountCodeReadRepository</c>.
/// </summary>
public sealed class AccountExceptionReadRepository : IAccountExceptionReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/>, EF Core translates it into a column-level SQL projection.
    /// </summary>
    private static readonly Expression<Func<TB_ACCOUNTEXCEPTION, AccountExceptionDto>> ToDto = a => new AccountExceptionDto(
        a.ID,
        a.ACCOUNTCOE_ID,
        a.VAHEDTYPE_ID,
        a.CREATEDDATE,
        a.UPDATEDDATE,
        a.ADDUSERID,
        a.CHANGEUSERID,
        a.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public AccountExceptionReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AccountExceptionDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_ACCOUNTEXCEPTIONs
            .AsNoTracking()
            .Where(a => a.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // No natural business key exists on this rule table, so rows are ordered by
            // creation time with ID as a pure tie-breaker for stable paging.
            .OrderBy(a => a.CREATEDDATE)
            .ThenBy(a => a.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<AccountExceptionDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<AccountExceptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_ACCOUNTEXCEPTIONs
            .AsNoTracking()
            .Where(a => a.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
