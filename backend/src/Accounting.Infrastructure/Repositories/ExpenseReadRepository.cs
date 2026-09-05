using System.Linq.Expressions;
using Accounting.Application.Expenses.Queries;
using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IExpenseReadRepository"/>.
///
/// Reads directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> rather than a
/// dedicated View/Materialized View — the same deliberate, narrow exception to the "reports
/// read from a Read Model" rule documented on <see cref="AccountCodeReadRepository"/>.
/// </summary>
public sealed class ExpenseReadRepository : IExpenseReadRepository
{
    /// <summary>
    /// Projection expression shared by both queries below. Being a literal
    /// <see cref="Expression"/> (not a compiled delegate/method call), EF Core can translate
    /// it into a column-level SQL projection instead of loading the full entity.
    /// </summary>
    private static readonly Expression<Func<TB_EXPENCE, ExpenseDto>> ToDto = e => new ExpenseDto(
        e.ID,
        e.EXPENCECODE,
        e.EXPENCENAME,
        e.DESCRIPTION,
        e.DEFAULTAMOUNT,
        e.EXPENCEGROUP_ID,
        e.ACCOUNTCODE_ID,
        e.VAHEDCODE,
        e.CREATEDDATE,
        e.UPDATEDDATE,
        e.ADDUSERID,
        e.CHANGEUSERID,
        e.ISDELETED);

    private readonly LegacyDbContext _dbContext;

    public ExpenseReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ExpenseDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Logical delete filter: ISDELETED is bool? in Legacy, so both false and NULL mean
        // "not deleted" — only an explicit true excludes the row.
        var query = _dbContext.TB_EXPENCEs
            .AsNoTracking()
            .Where(e => e.ISDELETED != true);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // EXPENCECODE alone is not unique (UK_EXPENSE_CODE is a composite over
            // EXPENCECODE+VAHEDCODE), so ID is a pure tie-breaker for stable paging.
            .OrderBy(e => e.EXPENCECODE)
            .ThenBy(e => e.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<ExpenseDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter here on purpose: GetById returns the row regardless of its
        // deletion state; the caller decides what to do based on ExpenseDto.IsDeleted.
        return _dbContext.TB_EXPENCEs
            .AsNoTracking()
            .Where(e => e.ID == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
