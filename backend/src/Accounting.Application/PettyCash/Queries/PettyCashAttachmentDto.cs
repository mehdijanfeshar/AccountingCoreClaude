namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// One row of <c>GET api/petty-cash/expense-docs/{id}/attachments</c> — metadata only, never
/// <c>ATTACH_FILE</c> (CLAUDE.md rule 6 + <c>docs/tankhah-khazaneh-module.md</c>، تصمیم‌های بخش ۲،
/// پیوست: "لیست پیوست بدون بایت برمی‌گردد").
/// </summary>
public sealed record PettyCashAttachmentDto(
    Guid Id,
    Guid ExpenseDocId,
    string AttachName,
    int AttachSize,
    string? ContentType,
    int AttachRadif,
    string AddUserId,
    DateTime CreatedDate);
