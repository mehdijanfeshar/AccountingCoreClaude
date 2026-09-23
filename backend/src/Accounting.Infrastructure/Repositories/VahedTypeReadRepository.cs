using System.Linq.Expressions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.VahedTypes.Queries;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IVahedTypeReadRepository"/>.
/// </summary>
public sealed class VahedTypeReadRepository : IVahedTypeReadRepository
{
    /// <summary>
    /// Column-level projection. Being a literal <see cref="Expression"/> rather than a compiled
    /// delegate, EF Core translates it into the SELECT list instead of materialising entities.
    /// </summary>
    private static readonly Expression<Func<TB_VAHED_TYPE, VahedTypeDto>> ToDto = t => new VahedTypeDto(
        t.ID,
        t.TYPECODE,
        t.TYPENAME,
        t.PARENTTYPECODE);

    private readonly LegacyDbContext _dbContext;

    public VahedTypeReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<VahedTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // No ISDELETED filter: the table has no such column (see the interface's XML doc).
        //
        // TYPECODE is VARCHAR2, so this is a lexicographic sort — "10" sorts before "9". That is
        // accepted rather than worked around: the consumer groups by PARENTTYPECODE and renders
        // names, so the within-bucket order is cosmetic, and casting to a number in SQL would
        // throw on any non-numeric code the shared schema might hold.
        var items = await _dbContext.TB_VAHED_TYPEs
            .AsNoTracking()
            .OrderBy(t => t.PARENTTYPECODE)
            .ThenBy(t => t.TYPECODE)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return items;
    }
}
