using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// یادداشت توضیحی متنی (ح-۶، سند منبع §۹) برای صورت‌های یک واحد، مجموعه و سال. <see cref="CONTENT_JSON"/> =
/// سند Tiptap با متغیرهای <c>{{ROW}}</c>/<c>{{TPL/ROW.change%}}</c> که هنگام نمایش از Snapshot اجرا مقدار
/// می‌گیرند. <see cref="LINKED_TEMPLATE_CODE"/> = یادداشت عددی (۴۵-ج) که این متن همراهش می‌آید؛ خالی = یادداشت
/// متنی مستقل (تاریخچه، مبنا، رویه‌ها). هر ذخیره یک نسخه در <c>TB_FS_NARRATIVE_VERSION</c> می‌سازد. DDL 061.
/// </summary>
public partial class TB_FS_NARRATIVE
{
    public Guid ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public FsFramework FRAMEWORK { get; set; }

    public string YEAR { get; set; } = null!;

    public int ORDER_NO { get; set; }

    public string TITLE_FA { get; set; } = null!;

    public string? LINKED_TEMPLATE_CODE { get; set; }

    public string? CONTENT_JSON { get; set; }

    public FsNarrativeState STATE { get; set; }

    public string? RESPONSIBLE_USERID { get; set; }

    public string? REVIEW_COMMENT { get; set; }

    public int VERSION_NO { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }
}
