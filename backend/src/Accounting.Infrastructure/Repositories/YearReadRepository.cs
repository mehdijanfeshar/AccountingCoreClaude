using Accounting.Application.Common.Interfaces;
using Accounting.Application.Years.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IYearReadRepository"/>.
/// </summary>
public sealed class YearReadRepository : IYearReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public YearReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<YearDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Newest first: a year picker is almost always opened to reach the current or previous
        // year, not 1390. WORKING_YEAR is the primary key, so this ordering is already unique and
        // needs no tie-breaker.
        return await _dbContext.TB_YEARs
            .AsNoTracking()
            .OrderByDescending(y => y.WORKING_YEAR)
            .Select(y => new YearDto(y.WORKING_YEAR, y.ISCURRENT, y.LAST_NUMBER))
            .ToListAsync(cancellationToken);
    }
}
