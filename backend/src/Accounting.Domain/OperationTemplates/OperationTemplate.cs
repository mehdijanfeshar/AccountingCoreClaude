namespace Accounting.Domain.OperationTemplates;

// Agent-UX فاز ۱ — الگوی عملیات. جدول‌های جدید (DDL 067)، نه Legacy؛ برای همین کلاس‌ها نام
// خوانا دارند و نگاشت ستون‌های بزرگ اوراکل در Infrastructure/OperationTemplates انجام می‌شود.
// شناسه‌ها همان قرارداد پروژه‌اند: CHAR(36) ⇄ Guid.

/// <summary>
/// الگوی عملیات: تعریف حسابدار ستاد از اینکه یک عملیات روزمره
/// (مثل «دریافت وجه از مشتری») چه سندی تولید می‌کند.
/// سراسری است (واحد ندارد) و فقط ستاد آن را تعریف می‌کند.
/// </summary>
public class OperationTemplate
{
    public Guid Id { get; set; }

    /// <summary>کد یکتا و ثابت، برای ارجاع پایدار (مثلاً RECEIVE_FROM_CUSTOMER)</summary>
    public string Code { get; set; } = default!;

    /// <summary>عنوانی که کاربر غیرمالی می‌بیند: «دریافت وجه از مشتری»</summary>
    public string Title { get; set; } = default!;

    /// <summary>
    /// توضیح زبان طبیعی؛ در فاز Agent همین متن به LLM داده می‌شود
    /// تا بفهمد چه زمانی این الگو را انتخاب کند.
    /// </summary>
    public string Description { get; set; } = default!;

    /// <summary>
    /// کلمات کلیدی و جمله‌های نمونهٔ کاربر، هر کدام در یک خط («دارو»، «قرص خریدم»…). جستجوی حسابیار و
    /// در فاز Agent هوش مصنوعی الگو را با این‌ها هم پیدا می‌کند.
    /// </summary>
    public string? Keywords { get; set; }

    /// <summary>
    /// نوع واحدهایی که این الگو را می‌بینند (کد <c>TB_VAHEDTYPE.TYPECODE</c>، با «,» جدا)؛ null = همه. DDL 071.
    /// </summary>
    public string? AllowedVahedTypes { get; set; }

    /// <summary>الگوی شرح سند، مثل: «دریافت از {customer} - {note}»</summary>
    public string VoucherDescriptionPattern { get; set; } = default!;

    /// <summary>نوع سندی که ساخته می‌شود (<c>TB_SYSTYPE.ID</c> ⇒ <c>TB_VOUCHERSHEAD.SYSTEM_TYPE</c>)؛ null = بدون نوع.</summary>
    public Guid? SystemTypeId { get; set; }

    public bool IsActive { get; set; } = true;

    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }

    public List<TemplateParameter> Parameters { get; set; } = new();
    public List<TemplateLine> Lines { get; set; } = new();
}

public enum ParameterType
{
    /// <summary>مبلغ به ریال (عدد صحیح)</summary>
    Amount = 1,
    /// <summary>یک تفصیلی از یک گروه تفصیلی مشخص (شخص، بانک، صندوق، ...)</summary>
    Detail = 2,
    Text = 3,
    Date = 4
}

/// <summary>اطلاعاتی که برای اجرای الگو باید از کاربر گرفته شود.</summary>
public class TemplateParameter
{
    public Guid Id { get; set; }
    public Guid OperationTemplateId { get; set; }

    /// <summary>کلید لاتین و ثابت، مثلاً amount / customer / cashAccount</summary>
    public string Key { get; set; } = default!;

    /// <summary>عنوان فارسی: «مبلغ»، «مشتری»، «حساب دریافت»</summary>
    public string Title { get; set; } = default!;

    public ParameterType Type { get; set; }

    /// <summary>برای Type=Detail: گروه تفصیلی مجاز (<c>TB_TAFSIL_GROUP.ID</c>)</summary>
    public Guid? DetailGroupId { get; set; }

    public bool IsRequired { get; set; } = true;

    /// <summary>سؤالی که اگر این پارامتر کم بود از کاربر پرسیده می‌شود: «پول به کدام حساب وارد شد؟»</summary>
    public string AskPrompt { get; set; } = default!;

    public int SortOrder { get; set; }
}

public enum LineSide { Debit = 1, Credit = 2 }

/// <summary>یک ردیف سندی که الگو تولید می‌کند.</summary>
public class TemplateLine
{
    public Guid Id { get; set; }
    public Guid OperationTemplateId { get; set; }

    public LineSide Side { get; set; }

    /// <summary>حساب معین (<c>TB_ACCOUNTCODE.ID</c> با <c>TYPECODE=Moin</c>)</summary>
    public Guid SubsidiaryAccountId { get; set; }

    /// <summary>پارامتر مبلغی که این ردیف از آن محاسبه می‌شود</summary>
    public string AmountParameterKey { get; set; } = default!;

    /// <summary>درصد از مبلغ پارامتر (۱۰۰ = کل مبلغ). برای مالیات، کسورات و ...</summary>
    public decimal Percent { get; set; } = 100m;

    /// <summary>
    /// ردیف تراز‌کننده: مبلغش = اختلاف جمع بدهکار و بستانکار بقیهٔ ردیف‌ها.
    /// برای جذب اختلاف گرد کردن در الگوهای درصدی. حداکثر یکی در هر الگو.
    /// </summary>
    public bool IsBalancingLine { get; set; }

    /// <summary>الگوی شرح ردیف؛ خالی = شرح سند</summary>
    public string? DescriptionPattern { get; set; }

    public int SortOrder { get; set; }

    public List<TemplateLineDetail> Details { get; set; } = new();
}

/// <summary>تفصیلیِ هر سطح از ردیف: یا از پارامتر کاربر یا یک تفصیلی ثابت.</summary>
public class TemplateLineDetail
{
    public Guid Id { get; set; }
    public Guid TemplateLineId { get; set; }

    /// <summary>سطح تفصیلی (۱ تا ۷) — همان <c>TB_LEVEL_TAFSIL.LEVEL_CODE</c> سطح‌های معین</summary>
    public int Level { get; set; }

    /// <summary>از کدام پارامتر Detail پر شود</summary>
    public string? ParameterKey { get; set; }

    /// <summary>یا یک تفصیلی ثابت (مثلاً «سازمان امور مالیاتی») — <c>TB_TAFSILI.ID</c></summary>
    public Guid? FixedDetailId { get; set; }
}
