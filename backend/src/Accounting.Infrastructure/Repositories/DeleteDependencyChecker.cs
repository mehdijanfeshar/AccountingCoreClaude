using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// <see cref="IDeleteDependencyChecker"/> روی جدول‌های Legacy. <c>CountAsync &gt; 0</c> به‌جای
/// <c>AnyAsync</c> (ORA-00904 روی Oracle پیش از 23ai).
/// </summary>
public sealed class DeleteDependencyChecker : IDeleteDependencyChecker
{
    private readonly LegacyDbContext _db;

    public DeleteDependencyChecker(LegacyDbContext db)
    {
        _db = db;
    }

    public async Task<string?> FindAccountCodeBlockerAsync(Guid accountCodeId, CancellationToken cancellationToken = default)
    {
        if (await _db.TB_ACCOUNTCODEs.CountAsync(a => a.PARENTID == accountCodeId && a.ISDELETED != true, cancellationToken) > 0)
        {
            return "این حساب زیرحساب فعال دارد؛ اول زیرحساب‌ها را حذف کنید.";
        }

        if (await _db.TB_VOUCHERSDETAILs.CountAsync(d => d.ACCOUNT_ID == accountCodeId && d.ISDELETED != true, cancellationToken) > 0)
        {
            return "این حساب در ردیف سندها استفاده شده و قابل حذف نیست.";
        }

        if (await _db.TB_ACCOUNT_LINK_LEVELs.CountAsync(l => l.ACCOUNT_ID == accountCodeId && !l.ISDELETED, cancellationToken) > 0
            || await _db.TB_ACCOUNT_LINK_TAFSILGROUPs.CountAsync(l => l.ACCOUNT_ID == accountCodeId && !l.ISDELETED, cancellationToken) > 0
            || await _db.TB_ACCOUNT_LINK_TAFSILIs.CountAsync(l => l.ACCOUNT_ID == accountCodeId && !l.ISDELETED, cancellationToken) > 0)
        {
            return "برای این حساب سطح، گروه یا تفصیلی تعریف شده است؛ اول ارتباط‌های تفصیلی آن را حذف کنید.";
        }

        return null;
    }

    public async Task<string?> FindLevelTafsilBlockerAsync(Guid levelId, CancellationToken cancellationToken = default)
    {
        if (await _db.TB_VOUCHERDETAIL_LINK_TAFSILIs.CountAsync(l => l.LEVEL_ID == levelId && !l.ISDELETED, cancellationToken) > 0)
        {
            return "این سطح تفصیلی در ردیف سندها استفاده شده و قابل حذف نیست.";
        }

        if (await _db.TB_ACCOUNT_LINK_LEVELs.CountAsync(l => l.LEVEL_ID == levelId && !l.ISDELETED, cancellationToken) > 0
            || await _db.TB_ACCOUNT_LINK_TAFSILGROUPs.CountAsync(l => l.LEVEL_ID == levelId && !l.ISDELETED, cancellationToken) > 0
            || await _db.TB_ACCOUNT_LINK_TAFSILIs.CountAsync(l => l.LEVEL_ID == levelId && !l.ISDELETED, cancellationToken) > 0)
        {
            return "این سطح تفصیلی برای معین‌هایی تعریف شده است؛ اول از آن معین‌ها جدا کنید.";
        }

        return null;
    }

    public async Task<string?> FindTafsilGroupBlockerAsync(Guid tafsilGroupId, CancellationToken cancellationToken = default)
    {
        if (await _db.TB_TAFSIL_LINK_TAFSILGROUPs.CountAsync(l => l.TAFSILGROUP_ID == tafsilGroupId && !l.ISDELETED, cancellationToken) > 0)
        {
            return "این گروه تفصیلی عضو فعال دارد؛ اول تفصیلی‌ها را از گروه جدا کنید.";
        }

        if (await _db.TB_ACCOUNT_LINK_TAFSILGROUPs.CountAsync(l => l.TAFSILGROUP_ID == tafsilGroupId && !l.ISDELETED, cancellationToken) > 0)
        {
            return "این گروه تفصیلی به معین‌هایی وصل است؛ اول ارتباط معین با گروه را حذف کنید.";
        }

        return null;
    }

    public async Task<string?> FindBlockerAsync(DeleteGuardTarget target, Guid id, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;
        switch (target)
        {
            case DeleteGuardTarget.AttribForAccountCode:
                return await _db.TB_ATTRIBSINVOUCHERs.CountAsync(a => a.ATTRIBFORACCOUNTCODE_ID == id && a.ISDELETED != true, ct) > 0
                    ? "این ویژگی در ردیف سندها مقدار گرفته و قابل حذف نیست."
                    : null;

            case DeleteGuardTarget.ChequeType:
                return await _db.TB_CHECKBOOKs.CountAsync(c => c.CHECKTYPE_ID == id && c.ISDELETED != true, ct) > 0
                    ? "دسته‌چک‌هایی از این نوع تعریف شده است؛ اول نوع آن‌ها را عوض یا حذفشان کنید."
                    : null;

            case DeleteGuardTarget.Expense:
                if (await _db.TB_CHARGEANDCOST_DETAILs.CountAsync(d => d.EXPENSE_ID == id && d.ISDELETED != true, ct) > 0)
                    return "این هزینه در شارژ و هزینه استفاده شده و قابل حذف نیست.";
                return await _db.TB_EXPENCE_LINK_TAFSILIs.CountAsync(l => l.EXPENSE_ID == id && l.ISDELETED != true, ct) > 0
                    ? "برای این هزینه تفصیلی تعریف شده است؛ اول ارتباط‌های تفصیلی آن را حذف کنید."
                    : null;

            case DeleteGuardTarget.IdentityGroup:
                if (await _db.TB_IDENTITYSUBGRPs.CountAsync(s => s.IDENTYGROUPS_ID == id && s.ISDELETED != true, ct) > 0)
                    return "این گروه شناسه زیرگروه فعال دارد؛ اول زیرگروه‌ها را حذف کنید.";
                return await _db.TB_IDENTITYHEADs.CountAsync(h => h.IDENTITYGROUPS_ID == id && h.ISDELETED != true, ct) > 0
                    ? "برای این گروه شناسه ثبت شده است و قابل حذف نیست."
                    : null;

            case DeleteGuardTarget.IdentitySubGroup:
                return await _db.TB_IDENTITYDETAILs.CountAsync(d => d.IDENTITYSUBGRPS_ID == id && d.ISDELETED != true, ct) > 0
                       || await _db.TB_IDENTITYFIXITEMs.CountAsync(d => d.IDENTITYSUBGRPS_ID == id && d.ISDELETED != true, ct) > 0
                    ? "برای این زیرگروه مقدار ثبت شده است و قابل حذف نیست."
                    : null;

            case DeleteGuardTarget.Rabet:
            {
                var accountId = await _db.TB_RABETs.Where(r => r.ID == id).Select(r => r.ACCOUNTCODE_ID).FirstOrDefaultAsync(ct);
                return accountId is not null
                       && await _db.TB_VOUCHERSDETAILs.CountAsync(d => d.ACCOUNT_ID == accountId && d.ISDELETED != true, ct) > 0
                    ? "حساب این رابط گردش مالی دارد و رابط قابل حذف نیست."
                    : null;
            }

            case DeleteGuardTarget.RevolvingFund:
                return await _db.TB_CHARGEANDCOST_DETAILs.CountAsync(d => d.REVOLVINGFUND_ID == id && d.ISDELETED != true, ct) > 0
                    ? "این تنخواه در شارژ تنخواه استفاده شده و قابل حذف نیست."
                    : null;

            case DeleteGuardTarget.ElamHead:
            {
                var head = await _db.TB_ELAMHEADs.Where(h => h.ID == id).Select(h => new { h.VOUCHERSHEAD_ID, h.WEB_STAT }).FirstOrDefaultAsync(ct);
                if (head is null)
                    return null;
                if (head.VOUCHERSHEAD_ID is not null)
                    return "برای این اعلامیه سند صادر شده و قابل حذف نیست.";
                return head.WEB_STAT is not null and not (byte)ElamWebStat.CreateDramad and not (byte)ElamWebStat.CreateOther
                    ? "این اعلامیه از مرحلهٔ ثبت گذشته (ارسال/تأیید) و قابل حذف نیست."
                    : null;
            }

            case DeleteGuardTarget.PayReciveHead:
                return await _db.TB_PAYRECIVHEADs.CountAsync(h => h.ID == id && h.VOUCHERSHEAD_ID != null, ct) > 0
                    ? "برای این دریافت/پرداخت سند صادر شده و قابل حذف نیست."
                    : null;

            case DeleteGuardTarget.Tafsili:
                if (await _db.TB_VOUCHERDETAIL_LINK_TAFSILIs.CountAsync(l => l.TAFSILI_ID == id && !l.ISDELETED, ct) > 0)
                    return "این تفصیلی در ردیف سندها استفاده شده و قابل حذف نیست.";
                if (await _db.TB_ELAMDETAIL_LINK_TAFSILIs.CountAsync(l => l.TAFSILI_ID == id && l.ISDELETED != true, ct) > 0
                    || await _db.TB_PAYRECIVDETAIL_LINK_TAFSILIs.CountAsync(l => l.TAFSILI_ID == id && l.ISDELETED != true, ct) > 0)
                    return "این تفصیلی در اعلامیه یا دریافت/پرداخت استفاده شده و قابل حذف نیست.";
                return await _db.TB_ACCOUNT_LINK_TAFSILIs.CountAsync(l => l.TAFSILI_ID == id && !l.ISDELETED, ct) > 0
                       || await _db.TB_TAFSIL_LINK_TAFSILGROUPs.CountAsync(l => l.TAFSIL_ID == id && !l.ISDELETED, ct) > 0
                    ? "این تفصیلی به معین یا گروه تفصیلی وصل است؛ اول ارتباط‌هایش را حذف کنید."
                    : null;

            default:
                return null;
        }
    }
}
