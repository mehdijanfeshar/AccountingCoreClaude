using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashAttachments;

/// <summary>
/// <c>GET api/petty-cash/expense-docs/{id}/attachments</c> — metadata only, never
/// <c>ATTACH_FILE</c>. Anyone with VahedScope access to the parent صورت‌هزینه may call this — not
/// owner-only, unlike upload/delete (<c>docs/tankhah-khazaneh-module.md</c>، تصمیم‌های بخش ۲،
/// پیوست).
/// </summary>
public sealed record GetPettyCashAttachmentsQuery(Guid ExpenseDocId)
    : IRequest<IReadOnlyList<PettyCashAttachmentDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
