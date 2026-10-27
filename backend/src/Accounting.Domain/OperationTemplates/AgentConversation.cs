namespace Accounting.Domain.OperationTemplates;

/// <summary>
/// گفتگوی کاربر با دستیار حسابیار (فاز ۶۵، DDL 087). هر گفتگو مال یک کاربر در یک واحد است. دستیار فقط
/// ابزارهای خواندنی و «پیش‌نمایش سند» را صدا می‌زند؛ ثبت همیشه با کلیک کاربر در صفحهٔ حسابیار است.
/// </summary>
public class AgentConversation
{
    public Guid Id { get; set; }
    public string VahedCode { get; set; } = default!;

    /// <summary>چند کلمهٔ اول اولین پیام کاربر.</summary>
    public string Title { get; set; } = default!;

    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}

public enum AgentMessageRole
{
    User = 1,
    Assistant = 2,

    /// <summary>صدا زدن یک ابزار و نتیجه‌اش (فقط برای ردپا؛ به کاربر نشان داده نمی‌شود).</summary>
    Tool = 3,
}

/// <summary>یک پیام گفتگو (DDL 087). متن کاربر همان‌طور که نوشته ذخیره می‌شود (مثل جملهٔ اجرا)؛ ردپای ابزار پوشانده.</summary>
public class AgentMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }

    /// <summary>ترتیب پیام در گفتگو، از ۱.</summary>
    public int Seq { get; set; }

    public AgentMessageRole Role { get; set; }
    public string Content { get; set; } = default!;

    /// <summary>فقط پیام ابزار: نام ابزار.</summary>
    public string? ToolName { get; set; }

    /// <summary>فقط پیام دستیار: پیشنهاد سند (JSON) برای نمایش دوبارهٔ کارت هنگام باز کردن گفتگو.</summary>
    public string? ProposalJson { get; set; }

    /// <summary>فقط پیام دستیار: ردپای هوش مصنوعی همین نوبت (DDL 083).</summary>
    public Guid? AiTraceId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
