using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// تفصیلی(های) مرکز هزینهٔ یک درخواست پرداخت خزانه — یک ردیف به‌ازای هر سطح تفصیلی الزامی حساب
/// هزینهٔ درخواست (<see cref="TB_TR_PAYMENT_REQUEST.EXPENSE_ACCOUNT_ID"/>)، نه فقط سطح اول —
/// اصلاح ۴-الف (۲۰۲۶-۰۹-۲۹، صاحب پروژه؛ <c>docs/tankhah-khazaneh-module.md</c> §۱۰). هم‌شکل دقیق
/// <see cref="TB_PC_FUND_LINK_TAFSILI"/>، فقط <see cref="PAYMENT_REQUEST_ID"/> به‌جای
/// <c>FUND_ID</c>. جایگزین ستون تک‌سطحی حذف‌شدهٔ <c>TB_TR_PAYMENT_REQUEST.COST_CENTER_TAFSILI_ID</c>.
///
/// <b>جدول permanently-embedded</b> — مثل هر <c>*_LINK_TAFSIL*</c> دیگر پروژه (قاعدهٔ تیمی،
/// <c>docs/tamin-core-entity-reference.md</c> بخش ۵؛ <c>NoIndependentLinkTableWritePathTests</c>
/// اجرایش می‌کند): بدون <c>AddAsync</c>/<c>GetForUpdateAsync</c> به شکل aggregate-root روی
/// ریپازیتوری خودش. نوشتنش فقط از طریق متدهای صریح‌نام‌گذاری‌شده روی
/// <see cref="TB_TR_PAYMENT_REQUEST"/>'s repository
/// (<c>IPaymentRequestRepository.AddCostCenterTafsiliLinkAsync</c>/
/// <c>GetActiveCostCenterTafsiliLinksAsync</c>) انجام می‌شود؛ الزام واقعی سطح‌ها با
/// <c>IVoucherTafsiliLevelGuard.EnsureSatisfiedAsync</c> کنترل می‌شود (همان مکانیزم سند حسابداری).
///
/// <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱)؛ همیشه application-side تولید می‌شود.
/// </summary>
public partial class TB_TR_PAYMENT_REQUEST_LINK_TAFSILI
{
    public Guid ID { get; set; }

    public Guid PAYMENT_REQUEST_ID { get; set; }

    public Guid TAFSILI_ID { get; set; }

    public Guid LEVEL_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public virtual TB_TR_PAYMENT_REQUEST? PAYMENT_REQUEST { get; set; }

    public virtual TB_LEVEL_TAFSIL? LEVEL { get; set; }

    public virtual TB_TAFSILI? TAFSILI { get; set; }
}
