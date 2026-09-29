using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ITreasuryReceiptReadRepository"/>. A single flat
/// table (unlike a multi-join petty-cash read model) — same shape as
/// <c>PaymentRequestReadRepository</c>.
/// </summary>
public sealed class TreasuryReceiptReadRepository : ITreasuryReceiptReadRepository
{
    private readonly LegacyDbContext _dbContext;
    private readonly IVoucherAccountingReader _voucherAccountingReader;

    public TreasuryReceiptReadRepository(LegacyDbContext dbContext, IVoucherAccountingReader voucherAccountingReader)
    {
        _dbContext = dbContext;
        _voucherAccountingReader = voucherAccountingReader;
    }

    private IQueryable<TB_TR_RECEIPT> FilteredQuery(string vahedCode, string? search)
    {
        var query = _dbContext.TB_TR_RECEIPTs.AsNoTracking()
            .Where(r => !r.ISDELETED && r.VAHEDCODE == vahedCode);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.CODE.Contains(term) || r.PAYER_NAME.Contains(term));
        }

        return query;
    }

    public async Task<ReceiptListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        ReceiptState? state,
        string? search,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var scoped = FilteredQuery(vahedCode, search);

        var stateCounts = await scoped
            .GroupBy(r => r.STATE)
            .Select(g => new ReceiptStateCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var filtered = state is { } s ? scoped.Where(r => r.STATE == s) : scoped;

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .OrderByDescending(r => r.CREATEDDATE)
            .ThenByDescending(r => r.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReceiptListItemDto(
                r.ID, r.CODE, r.PAYER_NAME, r.AMOUNT, r.STATE, r.RECEIPT_DATE, r.CREATEDDATE, r.ADDUSERID))
            .ToListAsync(cancellationToken);

        return new ReceiptListResult(
            new PagedResult<ReceiptListItemDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
            },
            stateCounts);
    }

    public async Task<ReceiptDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var ownerVahedCode = await _dbContext.TB_TR_RECEIPTs
            .AsNoTracking()
            .Where(r => r.ID == id)
            .Select(r => r.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "Receipt");

        var entity = await _dbContext.TB_TR_RECEIPTs
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ID == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var payer = await _dbContext.TB_TAFSILIs
            .AsNoTracking()
            .Where(t => t.ID == entity.PAYER_TAFSILI_ID)
            .Select(t => new { t.TAFSILI_CODE, t.TAFSILI_NAME })
            .FirstOrDefaultAsync(cancellationToken);

        var voucherNumber = await GetVoucherDocNumAsync(entity.VOUCHER_ID, cancellationToken);

        string? payRecivCode = null;

        if (entity.PAYRECIVHEAD_ID is { } payRecivHeadId)
        {
            payRecivCode = await _dbContext.TB_PAYRECIVHEADs
                .AsNoTracking()
                .Where(h => h.ID == payRecivHeadId)
                .Select(h => h.PAYRECIVCODE)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ReceiptDto(
            entity.ID,
            entity.CODE,
            entity.PAYER_TAFSILI_ID,
            payer?.TAFSILI_CODE,
            payer?.TAFSILI_NAME,
            entity.PAYER_NAME,
            entity.AMOUNT,
            entity.BANK_ACCOUNT_ID,
            entity.RECEIPT_METHOD,
            entity.RECEIPT_DATE,
            entity.BANK_REFERENCE,
            entity.INVOICE_REF,
            entity.DESCRIPTION,
            entity.STATE,
            entity.VOUCHER_ID,
            voucherNumber,
            entity.PAYRECIVHEAD_ID,
            payRecivCode,
            entity.REGISTERED_BY,
            entity.REGISTERED_DATE,
            entity.ADDUSERID,
            entity.CREATEDDATE);
    }

    public async Task<ReceiptAccountingDto?> GetAccountingAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_TR_RECEIPTs
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ID == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        VahedOwnership.EnsureOwned(entity.VAHEDCODE, vahedCode, id, "Receipt");

        var voucher = await _voucherAccountingReader.GetAsync(entity.VOUCHER_ID, cancellationToken);

        string? payRecivCode = null;

        if (entity.PAYRECIVHEAD_ID is { } payRecivHeadId)
        {
            payRecivCode = await _dbContext.TB_PAYRECIVHEADs
                .AsNoTracking()
                .Where(h => h.ID == payRecivHeadId)
                .Select(h => h.PAYRECIVCODE)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ReceiptAccountingDto(voucher, payRecivCode);
    }

    private async Task<string?> GetVoucherDocNumAsync(Guid? voucherHeadId, CancellationToken cancellationToken)
    {
        if (voucherHeadId is not { } id)
        {
            return null;
        }

        return await _dbContext.TB_VOUCHERSHEADs
            .AsNoTracking()
            .Where(v => v.ID == id)
            .Select(v => v.DOC_NUM)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
