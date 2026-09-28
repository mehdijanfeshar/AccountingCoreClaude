using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashAttachment;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/attachments/{attId}/delete</c> — soft-deletes a
/// <c>TB_PC_ATTACHMENT</c> row. Same editability/ownership rules as
/// <c>UploadPettyCashAttachmentCommand</c>: only while the parent document is Draft/Returned, only
/// by the document's own owner. <see cref="ExpenseDocId"/> comes from the route only to
/// scope/validate the path (the attachment row must actually belong to that document); the row is
/// looked up and mutated by <see cref="Id"/> alone — same shape as
/// <c>DeletePettyCashFundReviewerCommand</c>.
/// </summary>
public sealed record DeletePettyCashAttachmentCommand(Guid ExpenseDocId, Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
