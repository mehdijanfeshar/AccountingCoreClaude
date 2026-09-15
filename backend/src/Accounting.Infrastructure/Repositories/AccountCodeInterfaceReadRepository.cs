using System.Linq.Expressions;
using Accounting.Application.AccountCodeInterfaces.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAccountCodeInterfaceReadRepository"/>. Reads
/// directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> — same narrow
/// exception to the "reports read from a Read Model" rule as <c>AccountCodeReadRepository</c>.
/// </summary>
public sealed class AccountCodeInterfaceReadRepository : IAccountCodeInterfaceReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/>, EF Core translates it into a column-level SQL projection.
    /// </summary>
    private static readonly Expression<Func<TB_ACCOUNTCODE_INTERFACE, AccountCodeInterfaceDto>> ToDto = a => new AccountCodeInterfaceDto(
        a.ID,
        a.TYPE,
        a.ACCOUNTCODEID,
        a.CREATEDDATE,
        a.UPDATEDDATE,
        a.ADDUSERID,
        a.CHANGEUSERID,
        a.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public AccountCodeInterfaceReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AccountCodeInterfaceDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is non-nullable bool here, so a plain != true check
        // is equivalent to == false, but kept in this form for consistency with the other
        // repositories in this project where the underlying column is nullable.
        var query = _dbContext.TB_ACCOUNTCODE_INTERFACEs
            .AsNoTracking()
            .Where(a => a.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // No natural business key exists on this link table, so rows are ordered by
            // creation time with ID as a pure tie-breaker for stable paging.
            .OrderBy(a => a.CREATEDDATE)
            .ThenBy(a => a.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<AccountCodeInterfaceDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<AccountCodeInterfaceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides based on AccountCodeInterfaceDto.IsDeleted.
        return _dbContext.TB_ACCOUNTCODE_INTERFACEs
            .AsNoTracking()
            .Where(a => a.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
