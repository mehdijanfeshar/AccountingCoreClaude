using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.UploadPettyCashAttachment;

/// <summary>
/// <c>POST api/petty-cash/expense-docs/{id}/attachments</c> (multipart/form-data) — adds a
/// <c>TB_PC_ATTACHMENT</c> row to a صورت‌هزینه. Only allowed while the document is
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Draft"/> or
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Returned"/>
/// (<see cref="Accounting.Application.Common.Security.PettyCashDocEditability"/>, reused — not
/// copied) and only by the document's own owner
/// (<see cref="Accounting.Application.Common.Interfaces.ICurrentUser.UserId"/> ==
/// <c>TB_PC_EXPENSE_DOC.ADDUSERID</c>; no reviewer/SoD involvement — this is not a بررسی action).
///
/// <c>Content</c> is <see langword="byte"/>[], not <c>IFormFile</c> — <c>Accounting.Application</c>
/// takes no dependency on ASP.NET Core (same boundary every other command in this project
/// respects), so <c>PettyCashController</c> reads the uploaded <c>IFormFile</c> and converts it to
/// bytes before building this command.
/// </summary>
public sealed record UploadPettyCashAttachmentCommand(
    Guid ExpenseDocId,
    string AttachName,
    string? ContentType,
    byte[] Content) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
