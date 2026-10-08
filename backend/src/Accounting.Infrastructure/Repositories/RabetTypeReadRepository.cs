using Accounting.Application.Rabets.Queries.GetRabetTypes;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class RabetTypeReadRepository(LegacyDbContext db) : IRabetTypeReadRepository
{
    public async Task<IReadOnlyList<RabetTypeDto>> ListAsync(CancellationToken ct)
        => await db.TB_RABET_TYPEs.AsNoTracking()
            .OrderBy(t => t.RABETCODE)
            .Select(t => new RabetTypeDto(t.ID, t.RABETCODE, t.TITLE))
            .ToListAsync(ct);
}
