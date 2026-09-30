using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasuryBankStatementRepository : ITreasuryBankStatementRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasuryBankStatementRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TR_BANK_STATEMENT statement, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TR_BANK_STATEMENTs.AddAsync(statement, cancellationToken);
    }

    public async Task<TB_TR_BANK_STATEMENT?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_TR_BANK_STATEMENTs.FirstOrDefaultAsync(s => s.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "BankStatement");

        return entity;
    }

    public async Task<int> GetNextCodeAsync(string vahedCode, string year, CancellationToken cancellationToken = default)
    {
        var existingCodes = await _dbContext.TB_TR_BANK_STATEMENTs
            .AsNoTracking()
            .Where(s => s.VAHEDCODE == vahedCode && s.YEAR == year)
            .Select(s => s.CODE)
            .ToListAsync(cancellationToken);

        var highest = 0;

        foreach (var code in existingCodes)
        {
            var numericPart = code.StartsWith("BST-", StringComparison.Ordinal) ? code["BST-".Length..] : code;

            if (int.TryParse(numericPart, out var parsed) && parsed > highest)
            {
                highest = parsed;
            }
        }

        return highest + 1;
    }
}
