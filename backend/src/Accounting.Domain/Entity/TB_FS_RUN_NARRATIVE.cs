using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// کپی یادداشت‌های توضیحی هنگام <b>انتشار</b> اجرا (ح-۶) تا متن صورت منتشرشده بعداً عوض نشود. فقط درج. DDL 061.
/// </summary>
public partial class TB_FS_RUN_NARRATIVE
{
    public Guid ID { get; set; }

    public Guid RUN_ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public Guid NARRATIVE_ID { get; set; }

    public int ORDER_NO { get; set; }

    public string TITLE_FA { get; set; } = null!;

    public string? LINKED_TEMPLATE_CODE { get; set; }

    public string? CONTENT_JSON { get; set; }

    public int VERSION_NO { get; set; }
}
