using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// تفصیلی(های) حساب معین یک تنخواه — ردیف بستانکار سند تسویهٔ دوره (بخش ۳-ب،
/// <c>docs/tankhah-khazaneh-module.md</c> §۹) از همین جدول تفصیلی می‌گیرد. هم‌شکل
/// <see cref="TB_REVOLVINGFUND_LINK_TAFSILI"/> Legacy، فقط با <see cref="FUND_ID"/> به
/// <see cref="TB_PC_FUND"/> (نه <c>TB_REVOLVING_FUND</c>) اشاره می‌کند — تصمیم ۲۰۲۶-۰۹-۲۸ (§۰).
///
/// <b>جدول permanently-embedded</b> — مثل هر <c>*_LINK_TAFSIL*</c> دیگر پروژه (قاعدهٔ تیمی،
/// <c>docs/tamin-core-entity-reference.md</c> بخش ۵؛ <c>NoIndependentLinkTableWritePathTests</c>
/// اجرایش می‌کند): بدون <c>AddAsync</c>/<c>GetForUpdateAsync</c> به شکل aggregate-root روی
/// ریپازیتوری خودش. نوشتنش فقط از طریق متدهای صریح‌نام‌گذاری‌شده روی
/// <see cref="TB_PC_FUND"/>'s repository (<c>IPettyCashFundRepository.AddFundTafsiliLinkAsync</c>/
/// <c>GetActiveFundTafsiliLinksAsync</c>) انجام می‌شود — همان الگوی
/// <c>IExpenseRepository.AddTafsiliLinkAsync</c>. endpoint عمومی
/// (<c>GET/POST api/petty-cash/funds/{fundId}/tafsilis</c>) از طریق <c>UpsertPettyCashFundTafsilisCommand</c>
/// (نامی که عمداً «LinkTafsili» ندارد تا در تطبیق نام تست‌های نگهبان گیر نکند) روی همین متدها
/// جایگزینی کامل انجام می‌دهد؛ خودِ جدول همچنان یک aggregate root مستقل نیست.
///
/// <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱)؛ همیشه application-side تولید می‌شود.
/// </summary>
public partial class TB_PC_FUND_LINK_TAFSILI
{
    public Guid ID { get; set; }

    public Guid FUND_ID { get; set; }

    public Guid TAFSILI_ID { get; set; }

    public Guid LEVEL_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public virtual TB_PC_FUND? FUND { get; set; }

    public virtual TB_LEVEL_TAFSIL? LEVEL { get; set; }

    public virtual TB_TAFSILI? TAFSILI { get; set; }
}
