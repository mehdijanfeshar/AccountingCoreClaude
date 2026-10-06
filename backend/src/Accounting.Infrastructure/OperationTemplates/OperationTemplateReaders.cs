using Accounting.Application.OperationTemplates;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>
/// معین‌ها + سطوح تفصیلی هر معین، فقط برای <c>ids</c> (یک کوئری روی هر جدول، نه به‌ازای هر معین).
/// زنجیره همان <c>TafsiliLookupReadRepository</c> است: <c>TB_ACCOUNT_LINK_LEVEL (+TB_LEVEL_TAFSIL)</c>
/// برای سطح‌ها و <c>TB_ACCOUNT_LINK_TAFSILGROUP</c> برای گروه‌های مجاز هر سطح. قاعدهٔ A پروژه:
/// هر سطح پیکربندی‌شده الزامی است.
/// </summary>
public sealed class SubsidiaryAccountReader : ISubsidiaryAccountReader
{
    private readonly LegacyDbContext _db;
    public SubsidiaryAccountReader(LegacyDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<Guid, SubsidiaryAccountInfo>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, SubsidiaryAccountInfo>();
        var idList = ids.ToList();

        var accounts = await _db.TB_ACCOUNTCODEs.AsNoTracking()
            .Where(a => idList.Contains(a.ID) && a.TYPECODE == TypeCodes.Moin)
            .Select(a => new { a.ID, a.ACCCODE, a.ACCCODENAME, a.ISDELETED })
            .ToListAsync(ct);

        var levels = await _db.TB_ACCOUNT_LINK_LEVELs.AsNoTracking()
            .Where(l => idList.Contains(l.ACCOUNT_ID) && l.ISDELETED == false && l.LEVEL.ISDELETED == false)
            .Select(l => new { l.ACCOUNT_ID, LevelId = l.LEVEL.ID, l.LEVEL.LEVEL_CODE })
            .ToListAsync(ct);

        var groups = await _db.TB_ACCOUNT_LINK_TAFSILGROUPs.AsNoTracking()
            .Where(g => idList.Contains(g.ACCOUNT_ID) && g.ISDELETED == false)
            .Select(g => new { g.ACCOUNT_ID, g.LEVEL_ID, g.TAFSILGROUP_ID })
            .ToListAsync(ct);

        var groupsByAccountLevel = groups
            .GroupBy(g => (g.ACCOUNT_ID, g.LEVEL_ID))
            .ToDictionary(g => g.Key, g => (IReadOnlySet<Guid>)g.Select(x => x.TAFSILGROUP_ID).ToHashSet());

        return accounts.ToDictionary(a => a.ID, a =>
        {
            // LEVEL_CODE رشته است؛ ردیف غیرعددی و تکراری کنار گذاشته می‌شود (همان رفتار TafsiliLookupReadRepository).
            var rules = levels.Where(l => l.ACCOUNT_ID == a.ID)
                .Select(l => (l.LevelId, Ok: int.TryParse(l.LEVEL_CODE, out var code), Code: code))
                .Where(l => l.Ok)
                .DistinctBy(l => l.LevelId)
                .OrderBy(l => l.Code)
                .Select(l => new DetailLevelRule(l.Code, l.LevelId,
                    groupsByAccountLevel.TryGetValue((a.ID, l.LevelId), out var g) ? g : new HashSet<Guid>(),
                    IsRequired: true))
                .ToList();

            return new SubsidiaryAccountInfo(a.ID, a.ACCCODE ?? "", a.ACCCODENAME ?? "", a.ISDELETED != true, rules);
        });
    }
}

/// <summary>
/// تفصیلی‌ها روی <c>ids</c> با فیلتر واحد: گروه‌های هر تفصیلی از <c>TB_TAFSIL_LINK_TAFSILGROUP</c>
/// و فقط لینک‌هایی که قاعدهٔ B برای واحد کاربر مجاز می‌داند (VAHEDCODE خود واحد، یا VAHEDTYPE = همه،
/// یا VAHEDTYPE = دستهٔ واحد کاربر) — عین <c>TafsiliLookupReadRepository.GetSelectableItemsAsync</c>.
/// IgnoreQueryFilters زده نمی‌شود.
/// </summary>
public sealed class DetailReader : IDetailReader
{
    private readonly LegacyDbContext _db;
    public DetailReader(LegacyDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<Guid, DetailInfo>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, string vahedCode, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, DetailInfo>();
        var idList = ids.ToList();

        var tafsilis = await _db.TB_TAFSILIs.AsNoTracking()
            .Where(t => idList.Contains(t.ID) && t.ISDELETED != true)
            .Select(t => new { t.ID, t.TAFSILI_CODE, t.TAFSILI_NAME, t.ISACTIVE })
            .ToListAsync(ct);

        var typeCode = await _db.TB_VAHED_INFOs.AsNoTracking()
            .Where(v => v.VAHEDCODE == vahedCode)
            .Select(v => v.VAHEDTYPE.TYPECODE)
            .FirstOrDefaultAsync(ct);
        var callerCategory = (short)VahedCategoryMapper.FromTypeCode(typeCode);
        var allCategory = (short)VahedCategory.All;

        var links = await _db.TB_TAFSIL_LINK_TAFSILGROUPs.AsNoTracking()
            .Where(l => idList.Contains(l.TAFSIL_ID) && l.ISDELETED == false)
            .Select(l => new
            {
                l.TAFSIL_ID,
                l.TAFSILGROUP_ID,
                Visible = l.VAHEDCODE == vahedCode || l.VAHEDTYPE == allCategory || l.VAHEDTYPE == callerCategory,
            })
            .ToListAsync(ct);

        return tafsilis.ToDictionary(t => t.ID, t =>
        {
            var visibleGroups = links.Where(l => l.TAFSIL_ID == t.ID && l.Visible)
                .Select(l => l.TAFSILGROUP_ID).ToHashSet();
            var hasAnyLink = links.Any(l => l.TAFSIL_ID == t.ID);

            return new DetailInfo(t.ID, t.TAFSILI_CODE ?? "", t.TAFSILI_NAME ?? "", visibleGroups,
                IsActive: t.ISACTIVE != TafsiliActiveState.DeActive,
                // بدون هیچ لینکی، خطای «گروه نادرست» گویاتر از «متعلق به واحد شما نیست» است.
                IsVisibleToUnit: visibleGroups.Count > 0 || !hasAnyLink);
        });
    }
}
