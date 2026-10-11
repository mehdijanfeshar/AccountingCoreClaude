using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) پیاده‌سازی <see cref="IFinancialYearRepository"/>. ذخیره با <c>IUnitOfWork</c> فراخوان.
/// ⚠️ هیچ مقایسهٔ بولی (ISDELETED / ISCURRENT) به SQL نمی‌رود — Oracle با ORA-00904 رد می‌کند؛ فیلتر در حافظه.
/// </summary>
public sealed class FinancialYearRepository : IFinancialYearRepository
{
    private readonly LegacyDbContext _dbContext;

    public FinancialYearRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<TB_YEAR>> GetAllForUpdateAsync(CancellationToken cancellationToken = default)
        => _dbContext.TB_YEARs.ToListAsync(cancellationToken);

    public void Add(TB_YEAR year) => _dbContext.TB_YEARs.Add(year);

    public async Task<IReadOnlyList<(Guid AccountId, string VahedCode)>> GetActiveBankAccountsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.TB_ACCOUNTs
            .AsNoTracking()
            .Select(a => new { a.ID, a.VAHEDCODE, a.ISDELETED })
            .ToListAsync(cancellationToken);

        return rows
            .Where(a => a.ISDELETED != true && !string.IsNullOrWhiteSpace(a.VAHEDCODE))
            .Select(a => (a.ID, a.VAHEDCODE!.Trim()))
            .ToList();
    }

    public Task<List<TB_CHECKBOOK>> GetSoriCheckBooksOfYearForUpdateAsync(string year, CancellationToken cancellationToken = default)
        => _dbContext.TB_CHECKBOOKs
            .Where(b => b.CHECKBOOK_TYPE == CheckType.Sori && b.FROMCHECKNUMBER.StartsWith(year))
            .ToListAsync(cancellationToken);

    public void AddCheckBook(TB_CHECKBOOK checkBook) => _dbContext.TB_CHECKBOOKs.Add(checkBook);
}
