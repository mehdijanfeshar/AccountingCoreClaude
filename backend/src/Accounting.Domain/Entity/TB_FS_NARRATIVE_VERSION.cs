using System;

namespace Accounting.Domain.Entity;

/// <summary>تاریخچهٔ نسخه‌های یک یادداشت توضیحی (ح-۶) — فقط درج. DDL 061.</summary>
public partial class TB_FS_NARRATIVE_VERSION
{
    public Guid ID { get; set; }

    public Guid NARRATIVE_ID { get; set; }

    public int VERSION_NO { get; set; }

    public string TITLE_FA { get; set; } = null!;

    public string? CONTENT_JSON { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }
}
