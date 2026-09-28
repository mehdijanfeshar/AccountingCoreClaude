using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashAttachmentFile;

/// <summary>
/// <c>GET api/petty-cash/expense-docs/{id}/attachments/{attId}/download</c> — the only place this
/// module returns <c>ATTACH_FILE</c> bytes. Same access rule as
/// <c>GetPettyCashAttachmentsQuery</c>: anyone with VahedScope access to the parent صورت‌هزینه, not
/// owner-only.
/// </summary>
public sealed record GetPettyCashAttachmentFileQuery(Guid ExpenseDocId, Guid AttachmentId)
    : IRequest<PettyCashAttachmentFileDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
