namespace Accounting.Domain.OperationTemplates;

public enum RequestStatus { Open = 1, Done = 2 }

/// <summary>
/// «موضوع را برای حسابدار بفرستید» (DDL 084): کاربری که الگو/سؤال مناسب پیدا نکرد، جملهٔ خود را برای
/// بررسی‌کنندگان واحد می‌فرستد. سند نمی‌سازد؛ فقط صف درخواست است.
/// </summary>
public class OperationRequest
{
    public Guid Id { get; set; }
    public string VahedCode { get; set; } = default!;
    public string Sentence { get; set; } = default!;

    /// <summary>کد ملی فرستنده</summary>
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Open;
    public string? HandledBy { get; set; }
    public DateTime? HandledAtUtc { get; set; }
    public string? Note { get; set; }
}
