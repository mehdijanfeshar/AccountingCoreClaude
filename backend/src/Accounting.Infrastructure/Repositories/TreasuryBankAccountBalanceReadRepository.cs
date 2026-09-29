using Accounting.Application.Common.Interfaces;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>See <see cref="ITreasuryBankAccountBalanceReadRepository"/> XML doc.</summary>
public sealed class TreasuryBankAccountBalanceReadRepository : ITreasuryBankAccountBalanceReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasuryBankAccountBalanceReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetBalanceAsync(
        Guid accountCodeId,
        IReadOnlyCollection<Guid> bankTafsiliIds,
        string vahedCode,
        string year,
        CancellationToken cancellationToken = default)
    {
        var tafsiliIds = bankTafsiliIds.Distinct().ToList();

        var query =
            from detail in _dbContext.TB_VOUCHERSDETAILs.AsNoTracking()
            join head in _dbContext.TB_VOUCHERSHEADs.AsNoTracking()
                on detail.VOUCHERSHEAD_ID equals head.ID
            where detail.ISDELETED != true
                  && head.ISDELETED != true
                  && detail.ACCOUNT_ID == accountCodeId
                  && detail.VAHEDCODE == vahedCode
                  && detail.YEAR == year
                  // temporary vouchers included on purpose — owner decision ۲۰۲۶-۰۹-۲۹، documented
                  // choice on ITreasuryBankAccountBalanceReadRepository.
            select detail;

        // A bank معین is normally shared by several bank accounts: keep only the lines that carry
        // every تفصیلی of this bank account. No links configured → whole معین (documented fallback).
        if (tafsiliIds.Count > 0)
        {
            var matchingDetailIds =
                from link in _dbContext.TB_VOUCHERDETAIL_LINK_TAFSILIs.AsNoTracking()
                where !link.ISDELETED && tafsiliIds.Contains(link.TAFSILI_ID)
                group link by link.VOUCHERSDETAIL_ID into g
                where g.Select(l => l.TAFSILI_ID).Distinct().Count() == tafsiliIds.Count
                select g.Key;

            query = query.Where(detail => matchingDetailIds.Contains(detail.ID));
        }

        return await query
            .Select(detail => (detail.DEBTOR ?? 0m) - (detail.CREDITOR ?? 0m))
            .SumAsync(cancellationToken);
    }
}
