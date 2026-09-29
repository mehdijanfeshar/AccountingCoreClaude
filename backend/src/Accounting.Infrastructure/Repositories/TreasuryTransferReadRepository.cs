using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasuryTransferReadRepository : ITreasuryTransferReadRepository
{
    private readonly LegacyDbContext _dbContext;
    private readonly IVoucherAccountingReader _voucherAccountingReader;

    public TreasuryTransferReadRepository(LegacyDbContext dbContext, IVoucherAccountingReader voucherAccountingReader)
    {
        _dbContext = dbContext;
        _voucherAccountingReader = voucherAccountingReader;
    }

    private IQueryable<TB_TR_TRANSFER> FilteredQuery(string vahedCode, string? search)
    {
        var query = _dbContext.TB_TR_TRANSFERs.AsNoTracking()
            .Where(t => !t.ISDELETED && t.VAHEDCODE == vahedCode);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.CODE.Contains(term) || t.REASON.Contains(term));
        }

        return query;
    }

    public async Task<TransferListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        TransferState? state,
        string? search,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var scoped = FilteredQuery(vahedCode, search);

        var stateCounts = await scoped
            .GroupBy(t => t.STATE)
            .Select(g => new TransferStateCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var filtered = state is { } s ? scoped.Where(t => t.STATE == s) : scoped;

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .OrderByDescending(t => t.CREATEDDATE)
            .ThenByDescending(t => t.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransferListItemDto(
                t.ID, t.CODE, t.SOURCE_BANK_ACCOUNT_ID, t.DEST_BANK_ACCOUNT_ID, t.AMOUNT, t.STATE, t.TRANSFER_DATE, t.CREATEDDATE, t.ADDUSERID))
            .ToListAsync(cancellationToken);

        return new TransferListResult(
            new PagedResult<TransferListItemDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
            },
            stateCounts);
    }

    public async Task<TransferDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var ownerVahedCode = await _dbContext.TB_TR_TRANSFERs
            .AsNoTracking()
            .Where(t => t.ID == id)
            .Select(t => t.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "Transfer");

        var entity = await _dbContext.TB_TR_TRANSFERs
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ID == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var events = await _dbContext.TB_TR_TRANSFER_EVENTs
            .AsNoTracking()
            .Where(e => e.TRANSFER_ID == id)
            .OrderBy(e => e.CREATEDDATE)
            .ThenBy(e => e.ID)
            .Select(e => new TransferEventDto(
                e.ID, e.ACTION, e.FROM_STATE, e.TO_STATE, e.NOTE, e.ADDUSERID, e.CREATEDDATE, e.CLIENT_IP))
            .ToListAsync(cancellationToken);

        var voucherNumber = await GetVoucherDocNumAsync(entity.VOUCHER_ID, cancellationToken);

        return new TransferDto(
            entity.ID,
            entity.CODE,
            entity.SOURCE_BANK_ACCOUNT_ID,
            entity.DEST_BANK_ACCOUNT_ID,
            entity.AMOUNT,
            entity.TRANSFER_DATE,
            entity.TRANSFER_METHOD,
            entity.REASON,
            entity.STATE,
            entity.BANK_REFERENCE,
            entity.VOUCHER_ID,
            voucherNumber,
            entity.APPROVED_BY,
            entity.APPROVED_DATE,
            entity.RETURN_REASON,
            entity.ADDUSERID,
            entity.CREATEDDATE,
            events);
    }

    public async Task<TransferAccountingDto?> GetAccountingAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_TR_TRANSFERs
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ID == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        VahedOwnership.EnsureOwned(entity.VAHEDCODE, vahedCode, id, "Transfer");

        var voucher = await _voucherAccountingReader.GetAsync(entity.VOUCHER_ID, cancellationToken);

        return new TransferAccountingDto(voucher);
    }

    public async Task<IReadOnlyList<TransferListItemDto>> GetPendingForCartableAsync(
        string vahedCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_TR_TRANSFERs
            .AsNoTracking()
            .Where(t => !t.ISDELETED && t.VAHEDCODE == vahedCode && t.STATE == TransferState.PendingTreasurer)
            .Select(t => new TransferListItemDto(
                t.ID, t.CODE, t.SOURCE_BANK_ACCOUNT_ID, t.DEST_BANK_ACCOUNT_ID, t.AMOUNT, t.STATE, t.TRANSFER_DATE, t.CREATEDDATE, t.ADDUSERID))
            .ToListAsync(cancellationToken);
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
