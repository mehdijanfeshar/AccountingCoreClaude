using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>See <see cref="ITreasuryBankStatementReadRepository"/> XML doc.</summary>
public sealed class TreasuryBankStatementReadRepository : ITreasuryBankStatementReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasuryBankStatementReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<TB_TR_BANK_STATEMENT> FilteredQuery(string vahedCode, Guid? bankAccountId)
    {
        var query = _dbContext.TB_TR_BANK_STATEMENTs.AsNoTracking()
            .Where(s => !s.ISDELETED && s.VAHEDCODE == vahedCode);

        if (bankAccountId is { } id)
        {
            query = query.Where(s => s.BANK_ACCOUNT_ID == id);
        }

        return query;
    }

    public async Task<BankStatementListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? bankAccountId,
        BankStatementState? state,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var scoped = FilteredQuery(vahedCode, bankAccountId);

        var stateCounts = await scoped
            .GroupBy(s => s.STATE)
            .Select(g => new BankStatementStateCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var filtered = state is { } s ? scoped.Where(x => x.STATE == s) : scoped;

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .OrderByDescending(x => x.CREATEDDATE)
            .ThenByDescending(x => x.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BankStatementListItemDto(
                x.ID, x.CODE, x.BANK_ACCOUNT_ID, x.FROM_DATE, x.TO_DATE, x.CLOSING_BALANCE, x.SOURCE, x.STATE, x.CREATEDDATE, x.ADDUSERID))
            .ToListAsync(cancellationToken);

        return new BankStatementListResult(
            new PagedResult<BankStatementListItemDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
            },
            stateCounts);
    }

    public async Task<BankStatementHeaderDto?> GetHeaderAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var ownerVahedCode = await _dbContext.TB_TR_BANK_STATEMENTs
            .AsNoTracking()
            .Where(s => s.ID == id)
            .Select(s => s.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "BankStatement");

        var entity = await _dbContext.TB_TR_BANK_STATEMENTs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ID == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return new BankStatementHeaderDto(
            entity.ID,
            entity.CODE,
            entity.BANK_ACCOUNT_ID,
            entity.FROM_DATE,
            entity.TO_DATE,
            entity.CLOSING_BALANCE,
            entity.SOURCE,
            entity.STATE,
            entity.DESCRIPTION,
            entity.ADDUSERID,
            entity.CREATEDDATE);
    }

    public async Task<IReadOnlyList<BankStatementLineDto>> GetLinesAsync(
        Guid statementId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var ownerVahedCode = await _dbContext.TB_TR_BANK_STATEMENTs
            .AsNoTracking()
            .Where(s => s.ID == statementId)
            .Select(s => s.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, statementId, "BankStatement");

        var lines = await _dbContext.TB_TR_BANK_STATEMENT_LINEs
            .AsNoTracking()
            .Where(l => l.STATEMENT_ID == statementId && !l.ISDELETED)
            .OrderBy(l => l.LINE_DATE)
            .ThenBy(l => l.ID)
            .ToListAsync(cancellationToken);

        if (lines.Count == 0)
        {
            return Array.Empty<BankStatementLineDto>();
        }

        // Batched label lookups — never N+1 per line (documented, unlike the کارتابل's per-fund
        // precedent, because the count here can be in the hundreds for a real bank statement).
        var matchedDetailIds = lines.Where(l => l.MATCHED_VOUCHERDETAIL_ID.HasValue)
            .Select(l => l.MATCHED_VOUCHERDETAIL_ID!.Value).Distinct().ToList();

        var matchedDocNumByDetailId = matchedDetailIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await (
                from detail in _dbContext.TB_VOUCHERSDETAILs.AsNoTracking()
                join head in _dbContext.TB_VOUCHERSHEADs.AsNoTracking() on detail.VOUCHERSHEAD_ID equals head.ID
                where matchedDetailIds.Contains(detail.ID)
                select new { detail.ID, head.DOC_NUM })
                .ToDictionaryAsync(x => x.ID, x => x.DOC_NUM, cancellationToken);

        var resolutionVoucherIds = lines.Where(l => l.RESOLUTION_VOUCHER_ID.HasValue)
            .Select(l => l.RESOLUTION_VOUCHER_ID!.Value).Distinct().ToList();

        var docNumByVoucherId = resolutionVoucherIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await _dbContext.TB_VOUCHERSHEADs.AsNoTracking()
                .Where(h => resolutionVoucherIds.Contains(h.ID))
                .ToDictionaryAsync(h => h.ID, h => h.DOC_NUM, cancellationToken);

        var resolutionReceiptIds = lines.Where(l => l.RESOLUTION_RECEIPT_ID.HasValue)
            .Select(l => l.RESOLUTION_RECEIPT_ID!.Value).Distinct().ToList();

        var codeByReceiptId = resolutionReceiptIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.TB_TR_RECEIPTs.AsNoTracking()
                .Where(r => resolutionReceiptIds.Contains(r.ID))
                .ToDictionaryAsync(r => r.ID, r => r.CODE, cancellationToken);

        return lines.Select(l => new BankStatementLineDto(
            l.ID,
            l.STATEMENT_ID,
            l.LINE_DATE,
            l.BANK_REFERENCE,
            l.DESCRIPTION,
            l.WITHDRAWAL,
            l.DEPOSIT,
            l.BALANCE,
            l.MATCH_STATE,
            l.MATCHED_VOUCHERDETAIL_ID,
            l.MATCHED_VOUCHERDETAIL_ID.HasValue && matchedDocNumByDetailId.TryGetValue(l.MATCHED_VOUCHERDETAIL_ID.Value, out var mdn) ? mdn : null,
            l.RESOLUTION_TYPE,
            l.RESOLUTION_VOUCHER_ID,
            l.RESOLUTION_VOUCHER_ID.HasValue && docNumByVoucherId.TryGetValue(l.RESOLUTION_VOUCHER_ID.Value, out var rdn) ? rdn : null,
            l.RESOLUTION_RECEIPT_ID,
            l.RESOLUTION_RECEIPT_ID.HasValue && codeByReceiptId.TryGetValue(l.RESOLUTION_RECEIPT_ID.Value, out var rc) ? rc : null,
            l.RESOLUTION_NOTE,
            l.ADDUSERID,
            l.CREATEDDATE))
            .ToList();
    }
}
