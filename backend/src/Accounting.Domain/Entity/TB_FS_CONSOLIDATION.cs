using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

// ط-۳ تا ط-۸ (docs/fs-module.md §۱۴) — جدول‌های تکمیل نهایی صورت‌های مالی. DDL 062.

/// <summary>
/// تنظیم یک مجموعه به‌صورت کلید/مقدار (مالکیت مثل قواعد کنترل: VAHEDCODE خالی = مشترک). کلیدها:
/// <c>CASH_SELECTOR</c>، <c>RESTATEMENT_SELECTOR</c>، <c>XBRL_SCHEMA_REF</c>، <c>XBRL_NAMESPACES</c>،
/// <c>XBRL_ENTITY_SCHEME</c>، <c>XBRL_ENTITY_ID</c>.
/// </summary>
public partial class TB_FS_SETTING
{
    public Guid ID { get; set; }

    public string? VAHEDCODE { get; set; }

    public FsFramework FRAMEWORK { get; set; }

    public string SETTING_KEY { get; set; } = null!;

    public string? SETTING_VALUE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }
}

/// <summary>درخت واحدها در لحظهٔ اجرا (ط-۳ «درخت نسخه‌دار»): واحدها و شرکت‌های تابعهٔ دامنه با والد و گروه.</summary>
public partial class TB_FS_RUN_UNIT
{
    public Guid ID { get; set; }

    public Guid RUN_ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string UNIT_CODE { get; set; } = null!;

    public string? UNIT_NAME { get; set; }

    public string? PARENT_CODE { get; set; }

    public string GROUP_CODE { get; set; } = null!;

    /// <summary>۱ = واحد سازمان، ۲ = شرکت تابعه.</summary>
    public int KIND { get; set; }
}

/// <summary>کاربرگ ترکیب/تلفیق (ط-۳): مبلغ هر ردیف صورت به تفکیک گروه (زیرواحد سطح اول، شرکت تابعه، ELIM).</summary>
public partial class TB_FS_RUN_ROW_GROUP
{
    public Guid ID { get; set; }

    public Guid RUN_ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public Guid RUN_ROW_ID { get; set; }

    public string GROUP_CODE { get; set; } = null!;

    public decimal? AMOUNT_CUR { get; set; }

    public decimal? AMOUNT_PRV { get; set; }
}

/// <summary>
/// قاعدهٔ حذف فی‌مابین (ط-۴، سند منبع §۸): دو سوی جفت‌حساب با انتخاب‌گر؛ در صورت ترکیبی/تلفیقی هر دو سو صفر
/// می‌شوند و اختلاف (جمع مانده‌ها) بیش از آستانه ⇒ V-07 مسدودکننده.
/// </summary>
public partial class TB_FS_ELIM_RULE
{
    public Guid ID { get; set; }

    public string? VAHEDCODE { get; set; }

    public FsFramework FRAMEWORK { get; set; }

    public string CODE { get; set; } = null!;

    public string TITLE_FA { get; set; } = null!;

    public string LEFT_SELECTOR { get; set; } = null!;

    public string RIGHT_SELECTOR { get; set; } = null!;

    public decimal TOLERANCE { get; set; }

    public bool IS_ACTIVE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }
}

/// <summary>نتیجهٔ یک قاعدهٔ حذف در اجرا (ط-۴). STATUS: ۱ تطبیق، ۲ در آستانه، ۳ عدم تطبیق، ۴ طرف مقابل ندارد.</summary>
public partial class TB_FS_RUN_ELIM
{
    public Guid ID { get; set; }

    public Guid RUN_ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string RULE_CODE { get; set; } = null!;

    public string TITLE_FA { get; set; } = null!;

    public decimal LEFT_AMOUNT { get; set; }

    public decimal RIGHT_AMOUNT { get; set; }

    public decimal DIFFERENCE { get; set; }

    public int STATUS { get; set; }
}

/// <summary>شرکت تابعه (ط-۵): کد ۱ تا ۴ نویسه (گروه در کاربرگ)، ارز و درصد مالکیت. مالک = واحد تلفیق‌کننده.</summary>
public partial class TB_FS_ENTITY
{
    public Guid ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string CODE { get; set; } = null!;

    public string TITLE_FA { get; set; } = null!;

    public string CURRENCY { get; set; } = null!;

    public decimal OWNERSHIP { get; set; }

    public bool IS_ACTIVE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }
}

/// <summary>
/// تراز آزمایشی واردشدهٔ شرکت تابعه برای یک دوره (ط-۵)، به ارز شرکت، نگاشت‌شده به کد معین سازمان.
/// ACC_CLASS: ۱ دارایی/بدهی، ۲ حقوق مالکانه، ۳ سود و زیان (نرخ میانگین).
/// </summary>
public partial class TB_FS_ENTITY_TB
{
    public Guid ID { get; set; }

    public Guid ENTITY_ID { get; set; }

    public string YEAR { get; set; } = null!;

    public int TO_MONTH { get; set; }

    public string ACCCODE { get; set; } = null!;

    public string? SOURCE_ACCCODE { get; set; }

    public string? SOURCE_ACCNAME { get; set; }

    public int ACC_CLASS { get; set; }

    public decimal OPENING_DEBTOR { get; set; }

    public decimal OPENING_CREDITOR { get; set; }

    public decimal PERIOD_DEBTOR { get; set; }

    public decimal PERIOD_CREDITOR { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;
}

/// <summary>نرخ تسعیر یک شرکت تابعه برای یک دوره (ط-۵، استاندارد ۱۶): ابتدا، پایان و میانگین.</summary>
public partial class TB_FS_ENTITY_RATE
{
    public Guid ID { get; set; }

    public Guid ENTITY_ID { get; set; }

    public string YEAR { get; set; } = null!;

    public int TO_MONTH { get; set; }

    public decimal OPENING_RATE { get; set; }

    public decimal CLOSING_RATE { get; set; }

    public decimal AVERAGE_RATE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;
}

/// <summary>نگاشت ردیف صورت به عنصر XBRL (ط-۸). PERIOD_TYPE: ۱ لحظه‌ای (instant)، ۲ دوره‌ای (duration).</summary>
public partial class TB_FS_XBRL_MAP
{
    public Guid ID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string TEMPLATE_CODE { get; set; } = null!;

    public string ROW_CODE { get; set; } = null!;

    public string ELEMENT { get; set; } = null!;

    public int PERIOD_TYPE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;
}
