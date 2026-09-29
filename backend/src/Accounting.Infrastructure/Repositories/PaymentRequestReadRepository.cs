using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPaymentRequestReadRepository"/>. Unlike the
/// petty-cash صورت‌هزینه read repository, <see cref="TB_TR_PAYMENT_REQUEST"/> is a single flat
/// table — <c>CODE</c>/<c>BENEFICIARY_NAME</c> live directly on it, so no join chain is needed for
/// filtering, counting or search.
/// </summary>
public sealed class PaymentRequestReadRepository : IPaymentRequestReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public PaymentRequestReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<TB_TR_PAYMENT_REQUEST> FilteredQuery(string vahedCode, string? search)
    {
        var query = _dbContext.TB_TR_PAYMENT_REQUESTs.AsNoTracking()
            .Where(p => !p.ISDELETED && p.VAHEDCODE == vahedCode);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.CODE.Contains(term) || p.BENEFICIARY_NAME.Contains(term));
        }

        return query;
    }

    public async Task<PaymentRequestListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        PaymentRequestState? state,
        string? search,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var scoped = FilteredQuery(vahedCode, search);

        // Computed with search applied but the state filter deliberately IGNORED, so one request
        // populates every کارتابل tab badge — same shape as PettyCashExpenseDocReadRepository.
        var stateCounts = await scoped
            .GroupBy(p => p.REQUEST_STATE)
            .Select(g => new PaymentRequestStateCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var filtered = state is { } s ? scoped.Where(p => p.REQUEST_STATE == s) : scoped;

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .OrderByDescending(p => p.CREATEDDATE)
            .ThenByDescending(p => p.ID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentRequestListItemDto(
                p.ID, p.CODE, p.BENEFICIARY_NAME, p.PAYMENT_TYPE, p.NET_PAYABLE_AMOUNT, p.REQUEST_STATE,
                p.DUE_DATE, p.SUBMITTED_DATE, p.CREATEDDATE, p.ADDUSERID))
            .ToListAsync(cancellationToken);

        return new PaymentRequestListResult(
            new PagedResult<PaymentRequestListItemDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
            },
            stateCounts);
    }

    public async Task<PaymentRequestDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var ownerVahedCode = await _dbContext.TB_TR_PAYMENT_REQUESTs
            .AsNoTracking()
            .Where(p => p.ID == id)
            .Select(p => p.VAHEDCODE)
            .FirstOrDefaultAsync(cancellationToken);

        VahedOwnership.EnsureOwned(ownerVahedCode, vahedCode, id, "PaymentRequest");

        var entity = await _dbContext.TB_TR_PAYMENT_REQUESTs
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ID == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var events = await _dbContext.TB_TR_PAYMENT_REQUEST_EVENTs
            .AsNoTracking()
            .Where(e => e.PAYMENT_REQUEST_ID == id)
            .OrderBy(e => e.CREATEDDATE)
            .ThenBy(e => e.ID)
            .Select(e => new PaymentRequestEventDto(
                e.ID, e.ACTION, e.FROM_STATE, e.TO_STATE, e.NOTE, e.ADDUSERID, e.CREATEDDATE, e.CLIENT_IP))
            .ToListAsync(cancellationToken);

        string? beneficiaryTafsiliCode = null;
        string? beneficiaryTafsiliName = null;

        if (entity.BENEFICIARY_TAFSILI_ID is { } beneficiaryTafsiliId)
        {
            var beneficiary = await _dbContext.TB_TAFSILIs
                .AsNoTracking()
                .Where(t => t.ID == beneficiaryTafsiliId)
                .Select(t => new { t.TAFSILI_CODE, t.TAFSILI_NAME })
                .FirstOrDefaultAsync(cancellationToken);

            beneficiaryTafsiliCode = beneficiary?.TAFSILI_CODE;
            beneficiaryTafsiliName = beneficiary?.TAFSILI_NAME;
        }

        var costCenterTafsilis = await (
            from link in _dbContext.TB_TR_PAYMENT_REQUEST_LINK_TAFSILIs.AsNoTracking()
            where link.PAYMENT_REQUEST_ID == id && !link.ISDELETED
            join level in _dbContext.TB_LEVEL_TAFSILs.AsNoTracking()
                on link.LEVEL_ID equals level.ID into levels
            from level in levels.DefaultIfEmpty()
            join tafsili in _dbContext.TB_TAFSILIs.AsNoTracking()
                on link.TAFSILI_ID equals tafsili.ID into tafsilis
            from tafsili in tafsilis.DefaultIfEmpty()
            orderby level.LEVEL_CODE
            select new PaymentRequestCostCenterTafsiliDto(
                link.LEVEL_ID,
                level.LEVEL_NAME,
                link.TAFSILI_ID,
                tafsili.TAFSILI_CODE,
                tafsili.TAFSILI_NAME))
            .ToListAsync(cancellationToken);

        return new PaymentRequestDto(
            entity.ID,
            entity.CODE,
            entity.BENEFICIARY_NAME,
            entity.BENEFICIARY_NATIONAL_ID,
            entity.BENEFICIARY_TAFSILI_ID,
            beneficiaryTafsiliCode,
            beneficiaryTafsiliName,
            entity.PAYMENT_TYPE,
            entity.INVOICE_REF,
            entity.INVOICE_APPROVED,
            entity.EXPENSE_ACCOUNT_ID,
            costCenterTafsilis,
            entity.AMOUNT_BEFORE_TAX,
            entity.VAT_PERCENT,
            entity.VAT_AMOUNT,
            entity.INSURANCE_DEDUCTION_PERCENT,
            entity.INSURANCE_DEDUCTION_AMOUNT,
            entity.NET_PAYABLE_AMOUNT,
            entity.DUE_DATE,
            entity.PAYMENT_ACCOUNT_ID,
            entity.PAYMENT_METHOD,
            entity.DESCRIPTION,
            entity.REQUEST_STATE,
            entity.SUBMITTED_DATE,
            entity.PAYRECIVHEAD_ID,
            entity.ADDUSERID,
            entity.CREATEDDATE,
            events);
    }

    public async Task<IReadOnlyList<PaymentRequestListItemDto>> GetPendingForCartableAsync(
        string vahedCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_TR_PAYMENT_REQUESTs
            .AsNoTracking()
            .Where(p => !p.ISDELETED && p.VAHEDCODE == vahedCode && (
                p.REQUEST_STATE == PaymentRequestState.PendingUnitManager ||
                p.REQUEST_STATE == PaymentRequestState.PendingFinanceManager ||
                p.REQUEST_STATE == PaymentRequestState.PendingCeo))
            .Select(p => new PaymentRequestListItemDto(
                p.ID, p.CODE, p.BENEFICIARY_NAME, p.PAYMENT_TYPE, p.NET_PAYABLE_AMOUNT, p.REQUEST_STATE,
                p.DUE_DATE, p.SUBMITTED_DATE, p.CREATEDDATE, p.ADDUSERID))
            .ToListAsync(cancellationToken);
    }
}
