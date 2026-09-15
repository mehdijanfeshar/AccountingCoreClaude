using Accounting.Application.Common.Interfaces;
using Accounting.Application.SysTypes.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ISysTypeReadRepository"/>.
///
/// No logical-delete filter and no unit filter: <c>TB_SYSTYPE</c> has neither an
/// <c>ISDELETED</c> nor a <c>VAHEDCODE</c> column — it is a static reference table shared by
/// every unit.
/// </summary>
public sealed class SysTypeReadRepository : ISysTypeReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public SysTypeReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SysTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.TB_SYSTYPEs
            .AsNoTracking()
            .OrderBy(s => s.SYS_COD)
            .Select(s => new SysTypeDto(s.ID, s.SYS_COD, s.SYS_NAME))
            .ToListAsync(cancellationToken);
}
