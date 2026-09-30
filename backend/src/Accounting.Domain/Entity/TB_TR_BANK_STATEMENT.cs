using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «صورت‌حساب بانکی» یک حساب بانکی در یک بازهٔ تاریخی (<c>BST-xxxxxx</c>) — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۹). می‌تواند دستی وارد شود
/// (<see cref="SOURCE"/> = <see cref="BankStatementSource.Manual"/>) یا — وقتی صاحب پروژه قالب
/// فایل دیسکت بانک را بدهد — از فایل import شود (<see cref="BankStatementSource.Import"/>؛ فعلاً
/// هیچ <c>IBankStatementFileParser</c>ای ثبت نشده، endpoint import با ۴۰۹ رد می‌شود).
///
/// هیچ ستون شناسه‌ای اینجا FK واقعی در EF ندارد (همان الگوی ریسک باز #۹/#۱۴).
/// </summary>
public partial class TB_TR_BANK_STATEMENT
{
    public Guid ID { get; set; }

    /// <summary>نمایش کامل («<c>BST-</c>» + شمارندهٔ ۶رقمی به‌ازای (VAHEDCODE, YEAR)).</summary>
    public string CODE { get; set; } = null!;

    /// <summary>FK به <c>TB_ACCOUNT</c> — حساب بانکی این صورت‌حساب.</summary>
    public Guid BANK_ACCOUNT_ID { get; set; }

    /// <summary>شمسی <c>YYYYMMDD</c>.</summary>
    public string FROM_DATE { get; set; } = null!;

    /// <summary>شمسی <c>YYYYMMDD</c>.</summary>
    public string TO_DATE { get; set; } = null!;

    /// <summary>مانده پایانی طبق صورت‌حساب بانک — برای مقایسه با موجودی دفتری در خلاصهٔ جزئیات.</summary>
    public decimal CLOSING_BALANCE { get; set; }

    public BankStatementSource SOURCE { get; set; }

    public BankStatementState STATE { get; set; }

    public string? DESCRIPTION { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }
}
