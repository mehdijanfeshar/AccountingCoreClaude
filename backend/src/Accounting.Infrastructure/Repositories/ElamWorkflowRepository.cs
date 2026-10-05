using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Elams;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>سمت نوشتن گردش اعلامیه — فقط stage؛ ذخیره با UnitOfWork.</summary>
public sealed class ElamWorkflowRepository : IElamWorkflowRepository
{
    private readonly LegacyDbContext _dbContext;

    public ElamWorkflowRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TB_ELAMHEAD?> GetWithDetailsForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var head = await _dbContext.TB_ELAMHEADs
            .Include(h => h.TB_ELAMDETAILs.Where(d => d.ISDELETED != true))
                .ThenInclude(d => d.TB_ELAMDETAIL_LINK_TAFSILIs.Where(l => l.ISDELETED != true))
            .FirstOrDefaultAsync(h => h.ID == id && h.ISDELETED != true, cancellationToken);
        VahedOwnership.EnsureOwned(head?.VAHEDCODE, vahedCode, id, "Elam");
        return head;
    }

    public async Task<string> GetNextSerialAsync(string vahedCode, string year, string elamCode, CancellationToken cancellationToken = default)
    {
        // حذف‌شده‌ها هم شمرده می‌شوند: ایندکس یکتای (SERIALNO, CODE, VAHEDCODE) آن‌ها را هم دارد.
        var prefix = vahedCode + year;
        var max = await _dbContext.TB_ELAMHEADs
            .AsNoTracking()
            .Where(h => h.VAHEDCODE == vahedCode && h.YEAR == year && h.ELAMH_CODE == elamCode
                && h.ELAMH_SERIALNO != null && h.ELAMH_SERIALNO.StartsWith(prefix))
            .MaxAsync(h => h.ELAMH_SERIALNO, cancellationToken);
        var next = max is { Length: 14 } && long.TryParse(max, out var n) ? n + 1 : long.Parse(prefix + "000001");
        return next.ToString("D14");
    }

    public async Task<ElamRabetAccount?> GetRabetAccountAsync(string rabetTypeCode, CancellationToken cancellationToken = default)
    {
        // مرجع FirstOrDefault بی‌ترتیب می‌گیرد؛ اینجا ترتیب کد معین تا نتیجه پایدار باشد.
        var row = await _dbContext.TB_RABETs
            .AsNoTracking()
            .Where(r => r.ISDELETED != true && r.RABETTYPE!.RABETCODE == rabetTypeCode && r.ACCOUNTCODE_ID != null)
            .OrderBy(r => r.ACCOUNTCODE!.ACCCODE)
            .Select(r => new { Id = r.ACCOUNTCODE_ID!.Value, Code = r.ACCOUNTCODE!.ACCCODE })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null || row.Code is null ? null : new ElamRabetAccount(row.Id, row.Code);
    }

    public Task<string?> GetVahedNameAsync(string vahedCode, CancellationToken cancellationToken = default)
        => _dbContext.TB_VAHED_INFOs
            .AsNoTracking()
            .Where(v => v.VAHEDCODE == vahedCode)
            .Select(v => (string?)v.VAHEDNAME)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddHeadAsync(TB_ELAMHEAD head, CancellationToken cancellationToken = default)
        => await _dbContext.TB_ELAMHEADs.AddAsync(head, cancellationToken);

    public async Task AddDetailAsync(TB_ELAMDETAIL detail, CancellationToken cancellationToken = default)
        => await _dbContext.TB_ELAMDETAILs.AddAsync(detail, cancellationToken);

    public async Task AddDetailLinkAsync(TB_ELAMDETAIL_LINK_TAFSILI link, CancellationToken cancellationToken = default)
        => await _dbContext.TB_ELAMDETAIL_LINK_TAFSILIs.AddAsync(link, cancellationToken);

    public async Task<Guid?> GetAttribDefinitionIdAsync(Guid accountId, string vahedCode, string year, CancellationToken cancellationToken = default)
        => await _dbContext.TB_ATTRIBFORACCOUNTCODEs
            .AsNoTracking()
            .Where(a => a.ISDELETED != true && a.ACCOUNTCODE_ID == accountId && a.VAHEDCODE == vahedCode && a.YEAR == year)
            .Select(a => (Guid?)a.ID)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAttribInVoucherAsync(TB_ATTRIBSINVOUCHER row, CancellationToken cancellationToken = default)
        => await _dbContext.TB_ATTRIBSINVOUCHERs.AddAsync(row, cancellationToken);
}

/// <summary>سمت خواندن اعلامیه.</summary>
public sealed class ElamWorkflowReadRepository : IElamWorkflowReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public ElamWorkflowReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ElamCartableItemDto>> GetCartableAsync(ElamCartableFilter f, CancellationToken cancellationToken = default)
    {
        var (lo, hi) = f.Kind switch
        {
            ElamKind.Revenue => ((byte)1, (byte)4),
            ElamKind.Received => ((byte)9, (byte)10),
            _ => ((byte)5, (byte)8),
        };

        var q = _dbContext.TB_ELAMHEADs
            .AsNoTracking()
            .Where(h => h.ISDELETED != true && h.VAHEDCODE == f.VahedCode && h.YEAR == f.Year
                && h.WEB_STAT >= lo && h.WEB_STAT <= hi);

        var prefix = f.VahedCode + f.Year;
        if (f.SerialFrom is not null)
        {
            var from = PadSerial(prefix, f.SerialFrom);
            q = q.Where(h => string.Compare(h.ELAMH_SERIALNO, from) >= 0);
        }
        if (f.SerialTo is not null)
        {
            var to = PadSerial(prefix, f.SerialTo);
            q = q.Where(h => string.Compare(h.ELAMH_SERIALNO, to) <= 0);
        }
        if (f.DateFrom is not null)
            q = q.Where(h => string.Compare(h.ELAMH_DATE, f.DateFrom) >= 0);
        if (f.DateTo is not null)
            q = q.Where(h => string.Compare(h.ELAMH_DATE, f.DateTo) <= 0);
        if (f.DabirNo is not null)
            q = q.Where(h => h.ELAMH_DABIRNO == f.DabirNo);
        if (f.CounterVahedCode is not null)
            q = q.Where(h => h.ELAMH_SENDRCVVAHED == f.CounterVahedCode);

        var total = await q.CountAsync(cancellationToken);
        var rows = await Project(q
                .OrderByDescending(h => h.ELAMH_SERIALNO)
                .ThenByDescending(h => h.CREATEDDATE)
                .Skip((f.PageNumber - 1) * f.PageSize)
                .Take(f.PageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<ElamCartableItemDto>
        {
            Items = rows.Select(r => r.ToDto()).ToList(),
            PageNumber = f.PageNumber,
            PageSize = f.PageSize,
            TotalCount = total,
        };
    }

    public async Task<ElamViewDto?> GetAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var owner = await _dbContext.TB_ELAMHEADs
            .AsNoTracking()
            .Where(h => h.ID == id && h.ISDELETED != true)
            .Select(h => new { h.VAHEDCODE })
            .FirstOrDefaultAsync(cancellationToken);
        if (owner is null)
            return null;
        VahedOwnership.EnsureOwned(owner.VAHEDCODE, vahedCode, id, "Elam");

        var row = await Project(_dbContext.TB_ELAMHEADs.AsNoTracking().Where(h => h.ID == id)).FirstAsync(cancellationToken);
        var extra = await _dbContext.TB_ELAMHEADs
            .AsNoTracking()
            .Where(h => h.ID == id)
            .Select(h => new
            {
                h.ELAMH_CODE,
                h.ELAMH_WORKSHOPCODE,
                h.ELAMH_WORKSHOPNAME,
                h.WORKSHOP_ID,
                h.ELAMH_RCVNO,
                h.ELAMH_RCVDT,
                h.ELAMH_LSTMON,
                h.ELAMH_YEAR,
                h.PEIMAN_NO,
                h.PAY_NO,
            })
            .FirstAsync(cancellationToken);

        var details = await _dbContext.TB_ELAMDETAILs
            .AsNoTracking()
            .Where(d => d.ELAMHEAD_ID == id && d.ISDELETED != true)
            .OrderBy(d => d.CREATEDDATE)
            .Select(d => new
            {
                d.ID,
                d.ACCOUNTCODE_ID,
                AccCode = d.ACCOUNTCODE!.ACCCODE,
                AccName = d.ACCOUNTCODE.ACCCODENAME,
                Debtor = d.DEBTOR ?? 0m,
                Creditor = d.CREDITOR ?? 0m,
                d.ELAMD_DESC,
                d.ELAM_ATRIBNO,
                Links = d.TB_ELAMDETAIL_LINK_TAFSILIs
                    .Where(l => l.ISDELETED != true && l.TAFSILI_ID != null && l.LEVEL_ID != null)
                    .Select(l => new
                    {
                        TafsiliId = l.TAFSILI_ID!.Value,
                        LevelId = l.LEVEL_ID!.Value,
                        LevelCode = l.LEVEL!.LEVEL_CODE,
                        Code = l.TAFSILI!.TAFSILI_CODE,
                        Name = l.TAFSILI.TAFSILI_NAME,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var head = row.ToDto();
        var canEdit = head.VoucherHeadId is null
            && (head.WebStat == (byte)ElamWebStat.CreateOther || head.WebStat == (byte)ElamWebStat.CreateDramad);

        return new ElamViewDto(
            head,
            extra.ELAMH_CODE,
            extra.ELAMH_WORKSHOPCODE,
            extra.ELAMH_WORKSHOPNAME,
            extra.WORKSHOP_ID,
            extra.ELAMH_RCVNO,
            extra.ELAMH_RCVDT,
            extra.ELAMH_LSTMON,
            extra.ELAMH_YEAR,
            extra.PEIMAN_NO,
            extra.PAY_NO,
            canEdit,
            details.Select(d => new ElamDetailViewDto(
                d.ID, d.ACCOUNTCODE_ID, d.AccCode, d.AccName, d.Debtor, d.Creditor, d.ELAMD_DESC, d.ELAM_ATRIBNO,
                d.Links
                    .OrderBy(l => l.LevelCode)
                    .Select(l => new ElamTafsiliViewDto(l.TafsiliId, l.LevelId, l.LevelCode, l.Code, l.Name))
                    .ToList()))
                .ToList());
    }

    public async Task<IReadOnlyList<ElamUnitDto>> GetUnitsAsync(string excludeVahedCode, CancellationToken cancellationToken = default)
        => await _dbContext.TB_VAHED_INFOs
            .AsNoTracking()
            .Where(v => v.VAHEDCODE != excludeVahedCode)
            .OrderBy(v => v.VAHEDCODE)
            .Select(v => new ElamUnitDto(v.VAHEDCODE, v.VAHEDNAME))
            .ToListAsync(cancellationToken);

    private IQueryable<CartableRow> Project(IQueryable<TB_ELAMHEAD> q)
        => q.Select(h => new CartableRow
        {
            Id = h.ID,
            WebStat = h.WEB_STAT,
            SerialNo = h.ELAMH_SERIALNO,
            Date = h.ELAMH_DATE,
            Description = h.ELAMH_DESC,
            Case = h.ELAMH_CASE,
            CounterVahedCode = h.ELAMH_SENDRCVVAHED,
            CounterVahedName = _dbContext.TB_VAHED_INFOs
                .Where(v => v.VAHEDCODE == h.ELAMH_SENDRCVVAHED)
                .Select(v => v.VAHEDNAME)
                .FirstOrDefault(),
            DabirNo = h.ELAMH_DABIRNO,
            DabirDate = h.ELAMH_DABIRDATE,
            Amount = h.TB_ELAMDETAILs
                .Where(d => d.ISDELETED != true)
                .Sum(d => (d.DEBTOR ?? 0m) + (d.CREDITOR ?? 0m)),
            VoucherHeadId = h.VOUCHERSHEAD_ID,
            VoucherNumber = h.VOUCHERSHEAD!.DOC_NUM,
            VoucherDate = h.VOUCHERSHEAD.DATE_DOC,
            VoucherDescription = h.VOUCHERSHEAD.HEAD_DESC,
            SenderElamId = h.ELAMSENDERID,
            RevenueType = h.ELAMHDRAMAD_TYPE,
        });

    private static string PadSerial(string prefix, string input)
        => input.Length >= 14 ? input : prefix + input.PadLeft(6, '0');

    private sealed class CartableRow
    {
        public Guid Id { get; init; }
        public byte? WebStat { get; init; }
        public string? SerialNo { get; init; }
        public string? Date { get; init; }
        public string? Description { get; init; }
        public ElamCase? Case { get; init; }
        public string? CounterVahedCode { get; init; }
        public string? CounterVahedName { get; init; }
        public string? DabirNo { get; init; }
        public string? DabirDate { get; init; }
        public decimal Amount { get; init; }
        public Guid? VoucherHeadId { get; init; }
        public string? VoucherNumber { get; init; }
        public string? VoucherDate { get; init; }
        public string? VoucherDescription { get; init; }
        public Guid? SenderElamId { get; init; }
        public DaramElamhType? RevenueType { get; init; }

        public ElamCartableItemDto ToDto() => new(
            Id, ElamWorkflowService.KindOf(WebStat), SerialNo, Date, Description, Case, WebStat, CounterVahedCode,
            CounterVahedName, DabirNo, DabirDate, Amount, VoucherHeadId, VoucherNumber, VoucherDate,
            VoucherDescription, SenderElamId, RevenueType);
    }
}
