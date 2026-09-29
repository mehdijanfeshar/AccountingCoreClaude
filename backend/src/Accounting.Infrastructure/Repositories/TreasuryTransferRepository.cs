using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasuryTransferRepository : ITreasuryTransferRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasuryTransferRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TR_TRANSFER transfer, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TR_TRANSFERs.AddAsync(transfer, cancellationToken);
    }

    public async Task<TB_TR_TRANSFER?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_TR_TRANSFERs.FirstOrDefaultAsync(t => t.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "Transfer");

        return entity;
    }

    public async Task<int> GetNextCodeAsync(string vahedCode, string year, CancellationToken cancellationToken = default)
    {
        var existingCodes = await _dbContext.TB_TR_TRANSFERs
            .AsNoTracking()
            .Where(t => t.VAHEDCODE == vahedCode && t.YEAR == year)
            .Select(t => t.CODE)
            .ToListAsync(cancellationToken);

        var highest = 0;

        foreach (var code in existingCodes)
        {
            var numericPart = code.StartsWith("TRF-", StringComparison.Ordinal) ? code["TRF-".Length..] : code;

            if (int.TryParse(numericPart, out var parsed) && parsed > highest)
            {
                highest = parsed;
            }
        }

        return highest + 1;
    }

    public async Task<decimal> GetExecutedAmountForSourceOnDateAsync(
        Guid sourceBankAccountId,
        string transferDate,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_TR_TRANSFERs
            .AsNoTracking()
            .Where(t => !t.ISDELETED
                        && t.STATE == TransferState.Executed
                        && t.SOURCE_BANK_ACCOUNT_ID == sourceBankAccountId
                        && t.TRANSFER_DATE == transferDate);

        if (excludeId is { } id)
        {
            query = query.Where(t => t.ID != id);
        }

        return await query.SumAsync(t => (decimal?)t.AMOUNT, cancellationToken) ?? 0m;
    }
}
