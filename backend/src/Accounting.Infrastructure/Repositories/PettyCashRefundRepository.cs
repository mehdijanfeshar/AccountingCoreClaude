using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPettyCashRefundRepository"/>. Only stages
/// changes — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class PettyCashRefundRepository : IPettyCashRefundRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashRefundRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PC_REFUND refund, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PC_REFUNDs.AddAsync(refund, cancellationToken);
    }

    public async Task<TB_PC_REFUND?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_PC_REFUNDs
            .FirstOrDefaultAsync(r => r.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PettyCashRefund");

        return entity;
    }

    public async Task<decimal> GetTotalAsync(Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var amounts = await _dbContext.TB_PC_REFUNDs
            .AsNoTracking()
            .Where(r => r.FUND_ID == fundId && r.VAHEDCODE == vahedCode && !r.ISDELETED)
            .Select(r => r.AMOUNT)
            .ToListAsync(cancellationToken);

        return amounts.Sum();
    }

    public async Task<int> GetNextCodeAsync(string vahedCode, CancellationToken cancellationToken = default)
    {
        const string Prefix = "REF-";

        var existingCodes = await _dbContext.TB_PC_REFUNDs
            .AsNoTracking()
            .Where(r => r.VAHEDCODE == vahedCode)
            .Select(r => r.CODE)
            .ToListAsync(cancellationToken);

        var highest = 0;

        foreach (var code in existingCodes)
        {
            if (code.StartsWith(Prefix, StringComparison.Ordinal)
                && int.TryParse(code.AsSpan(Prefix.Length), out var parsed)
                && parsed > highest)
            {
                highest = parsed;
            }
        }

        return highest + 1;
    }
}
