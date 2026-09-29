using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasuryReceiptRepository : ITreasuryReceiptRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasuryReceiptRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TR_RECEIPT receipt, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TR_RECEIPTs.AddAsync(receipt, cancellationToken);
    }

    public async Task<TB_TR_RECEIPT?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_TR_RECEIPTs.FirstOrDefaultAsync(r => r.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "Receipt");

        return entity;
    }

    public async Task<int> GetNextCodeAsync(string vahedCode, string year, CancellationToken cancellationToken = default)
    {
        var existingCodes = await _dbContext.TB_TR_RECEIPTs
            .AsNoTracking()
            .Where(r => r.VAHEDCODE == vahedCode && r.YEAR == year)
            .Select(r => r.CODE)
            .ToListAsync(cancellationToken);

        var highest = 0;

        foreach (var code in existingCodes)
        {
            var numericPart = code.StartsWith("RCV-", StringComparison.Ordinal) ? code["RCV-".Length..] : code;

            if (int.TryParse(numericPart, out var parsed) && parsed > highest)
            {
                highest = parsed;
            }
        }

        return highest + 1;
    }

    public async Task<bool> ExistsDuplicateBankReferenceAsync(
        Guid bankAccountId,
        string bankReference,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_TR_RECEIPTs
            .AsNoTracking()
            .Where(r => !r.ISDELETED
                        && r.STATE != ReceiptState.Cancelled
                        && r.BANK_ACCOUNT_ID == bankAccountId
                        && r.BANK_REFERENCE == bankReference);

        if (excludeId is { } id)
        {
            query = query.Where(r => r.ID != id);
        }

        var count = await query.CountAsync(cancellationToken);

        return count > 0;
    }
}
