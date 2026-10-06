using Accounting.Application.Vouchers.Commands.Common;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// شناسه/ویژگی/فیش ردیف سند. فقط stage — SaveChanges با هندلر. روی Oracle از <c>AnyAsync</c>
/// استفاده نمی‌شود (<c>ORA-00904</c>) — <c>CountAsync</c>.
/// </summary>
public sealed class VoucherLineExtrasStore : IVoucherLineExtrasStore
{
    private readonly LegacyDbContext _db;

    public VoucherLineExtrasStore(LegacyDbContext db)
    {
        _db = db;
    }

    public async Task<VoucherLineRequirements> GetRequirementsAsync(
        Guid accountId, IReadOnlyCollection<Guid> tafsiliIds, string vahedCode, string year, CancellationToken ct)
    {
        var account = await _db.TB_ACCOUNTCODEs.AsNoTracking()
            .Where(a => a.ID == accountId)
            .Select(a => new { a.ID, a.PARENTID, a.IDENTYGROUPS_ID })
            .FirstOrDefaultAsync(ct);
        if (account is null)
            return new VoucherLineRequirements([], [], false);

        // شناسه روی خود معین؛ اگر معین تعریفی ندارد، روی حساب کل بالای آن.
        var attributes = await AttributesOfAsync(account.ID, vahedCode, year, ct);
        if (attributes.Count == 0 && account.PARENTID is { } parentId)
            attributes = await AttributesOfAsync(parentId, vahedCode, year, ct);

        var tafsiliList = tafsiliIds.Distinct().ToList();
        var accountGroupId = account.IDENTYGROUPS_ID;
        var groups = await _db.TB_IDENTITYGROUPs.AsNoTracking()
            .Where(g => g.ISDELETED != true && g.VAHEDCODE == vahedCode
                && ((g.TAFSILI_ID != null && tafsiliList.Contains(g.TAFSILI_ID.Value)) || (accountGroupId != null && g.ID == accountGroupId)))
            .Select(g => new { g.ID, g.IDENTITYGROUPS_DESC, g.TAFSILI_ID })
            .ToListAsync(ct);

        var groupIds = groups.Select(g => g.ID).ToList();
        var fields = groupIds.Count == 0
            ? []
            : await _db.TB_IDENTITYSUBGRPs.AsNoTracking()
                .Where(s => s.ISDELETED != true && groupIds.Contains(s.IDENTYGROUPS_ID))
                .OrderBy(s => s.IDENTYSUBGROUPS_CODE)
                .Select(s => new { s.ID, s.IDENTYGROUPS_ID, s.SUBGRPS_DESC, s.FIXED, s.SUBGRPS_TYPE, s.SUBGRPS_LEN })
                .ToListAsync(ct);

        var identities = groups
            .Select(g => new IdentityRequirement(
                g.ID,
                g.IDENTITYGROUPS_DESC,
                g.TAFSILI_ID,
                fields.Where(f => f.IDENTYGROUPS_ID == g.ID)
                    .Select(f => new IdentityFieldInfo(f.ID, f.SUBGRPS_DESC, f.FIXED, f.SUBGRPS_TYPE, f.SUBGRPS_LEN))
                    .ToList()))
            .ToList();

        var bankCount = await _db.TB_ACCOUNTs.AsNoTracking()
            .CountAsync(b => b.ACCOUNTCODE_ID == accountId && b.ISDELETED != true && b.VAHEDCODE == vahedCode, ct);

        return new VoucherLineRequirements(attributes, identities, bankCount > 0);
    }

    private async Task<List<AttributeRequirement>> AttributesOfAsync(Guid accountCodeId, string vahedCode, string year, CancellationToken ct)
    {
        var rows = await _db.TB_ATTRIBFORACCOUNTCODEs.AsNoTracking()
            .Where(a => a.ISDELETED != true && a.ACCOUNTCODE_ID == accountCodeId && a.VAHEDCODE == vahedCode && a.YEAR == year)
            .OrderBy(a => a.ATTRIBBOXNO)
            .Select(a => new
            {
                a.ID,
                a.ATTRIBBOXNO,
                a.FLAG,
                a.LENATR,
                a.CONTROLID,
                Code = a.ACCOUNTCODE.ACCCODE,
                Title = a.ACCOUNTCODE.ACCCODENAME,
            })
            .ToListAsync(ct);
        return rows
            .Select(a => new AttributeRequirement(a.ID, a.ATTRIBBOXNO, a.FLAG, a.LENATR, a.CONTROLID, a.Code ?? "", a.Title ?? ""))
            .ToList();
    }

    public async Task<IReadOnlyList<IdentityHeadOption>> ListHeadsAsync(Guid groupId, string vahedCode, string year, CancellationToken ct)
    {
        var heads = await _db.TB_IDENTITYHEADs.AsNoTracking()
            .Where(h => h.ISDELETED != true && h.IDENTITYGROUPS_ID == groupId && h.VAHEDCODE == vahedCode && h.YEAR == year)
            .OrderBy(h => h.SERIAL)
            .Select(h => new { h.ID, h.SERIAL })
            .Take(500)
            .ToListAsync(ct);
        var headIds = heads.Select(h => h.ID).ToList();
        var fixedValues = headIds.Count == 0
            ? []
            : await _db.TB_IDENTITYFIXITEMs.AsNoTracking()
                .Where(f => f.ISDELETED != true && headIds.Contains(f.IDENTITYHEAD_ID))
                .Select(f => new { f.IDENTITYHEAD_ID, f.IDENTITYSUBGRPS_ID, Title = f.IDENTITYSUBGRPS.SUBGRPS_DESC, f.FIXITEMS_VALUE })
                .ToListAsync(ct);
        return heads
            .Select(h => new IdentityHeadOption(
                h.ID,
                h.SERIAL,
                fixedValues.Where(f => f.IDENTITYHEAD_ID == h.ID)
                    .Select(f => new IdentityHeadFixedValue(f.IDENTITYSUBGRPS_ID, f.Title, f.FIXITEMS_VALUE))
                    .ToList()))
            .ToList();
    }

    public async Task<bool> HeadBelongsToGroupAsync(Guid headId, Guid groupId, string vahedCode, CancellationToken ct)
        => await _db.TB_IDENTITYHEADs.AsNoTracking()
            .CountAsync(h => h.ID == headId && h.IDENTITYGROUPS_ID == groupId && h.VAHEDCODE == vahedCode && h.ISDELETED != true, ct) > 0;

    public async Task<bool> ReceiptNoExistsAsync(
        ReceiptType kind, string no, string vahedCode, string year, Guid? exceptId, CancellationToken ct)
        => await _db.TB_RECEIPs.AsNoTracking()
            .CountAsync(r => r.ISDELETED != true && r.RECEIPT_KIND == kind && r.RECEIPT_NO == no
                && r.VAHEDCODE == vahedCode && r.YEAR == year && (exceptId == null || r.ID != exceptId), ct) > 0;

    public async Task<VoucherLineExtrasDto> GetSavedAsync(Guid detailId, string vahedCode, CancellationToken ct)
    {
        var line = await _db.TB_VOUCHERSDETAILs.AsNoTracking()
            .Where(d => d.ID == detailId && d.VAHEDCODE == vahedCode)
            .Select(d => new { d.RECEIP_ID })
            .FirstOrDefaultAsync(ct);
        if (line is null)
            return new VoucherLineExtrasDto([], [], null, null);

        var attributes = await _db.TB_ATTRIBSINVOUCHERs.AsNoTracking()
            .Where(a => a.VOUCHERSDETAIL_ID == detailId && a.ISDELETED != true)
            .Select(a => new VoucherLineAttributeInput(a.ATTRIBFORACCOUNTCODE_ID, a.ATTRIBUTEVALUE))
            .ToListAsync(ct);

        var details = await _db.TB_IDENTITYDETAILs.AsNoTracking()
            .Where(d => d.VOUCHERSDETAIL_ID == detailId && d.ISDELETED != true)
            .Select(d => new { d.IDENTITYHEAD_ID, GroupId = d.IDENTITYHEAD.IDENTITYGROUPS_ID, d.IDENTITYSUBGRPS_ID, d.DETAIL_VALUE })
            .ToListAsync(ct);
        var identities = details
            .GroupBy(d => new { d.GroupId, d.IDENTITYHEAD_ID })
            .Select(g => new VoucherLineIdentityInput(
                g.Key.GroupId,
                g.Key.IDENTITYHEAD_ID,
                g.Select(d => new VoucherLineIdentityValueInput(d.IDENTITYSUBGRPS_ID, d.DETAIL_VALUE)).ToList()))
            .ToList();

        VoucherLineReceiptInput? receipt = null;
        if (line.RECEIP_ID is { } rid)
        {
            receipt = await _db.TB_RECEIPs.AsNoTracking()
                .Where(r => r.ID == rid)
                .Select(r => new VoucherLineReceiptInput(r.RECEIPT_KIND, r.RECEIPT_NO, r.RECEIPT_DATE))
                .FirstOrDefaultAsync(ct);
        }

        return new VoucherLineExtrasDto(attributes, identities, line.RECEIP_ID, receipt);
    }

    public async Task SoftDeleteExtrasAsync(Guid detailId, string userId, DateTime now, CancellationToken ct)
    {
        var attributes = await _db.TB_ATTRIBSINVOUCHERs
            .Where(a => a.VOUCHERSDETAIL_ID == detailId && a.ISDELETED != true)
            .ToListAsync(ct);
        foreach (var a in attributes)
        {
            a.ISDELETED = true;
            a.CHANGEUSERID = userId;
            a.UPDATEDDATE = now;
        }

        var details = await _db.TB_IDENTITYDETAILs
            .Where(d => d.VOUCHERSDETAIL_ID == detailId && d.ISDELETED != true)
            .ToListAsync(ct);
        foreach (var d in details)
        {
            d.ISDELETED = true;
            d.CHANGEUSERID = userId;
            d.UPDATEDDATE = now;
        }
    }

    public async Task<TB_RECEIP?> GetReceiptForUpdateAsync(Guid receiptId, string vahedCode, CancellationToken ct)
        => await _db.TB_RECEIPs.FirstOrDefaultAsync(r => r.ID == receiptId && r.VAHEDCODE == vahedCode && r.ISDELETED != true, ct);

    public async Task AddAttributeAsync(TB_ATTRIBSINVOUCHER row, CancellationToken ct)
        => await _db.TB_ATTRIBSINVOUCHERs.AddAsync(row, ct);

    public async Task AddIdentityDetailAsync(TB_IDENTITYDETAIL row, CancellationToken ct)
        => await _db.TB_IDENTITYDETAILs.AddAsync(row, ct);

    public async Task AddReceiptAsync(TB_RECEIP row, CancellationToken ct)
        => await _db.TB_RECEIPs.AddAsync(row, ct);
}
