namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// Single-attachment shape for <c>GET api/petty-cash/expense-docs/{id}/attachments/{attId}/download</c>
/// — the only place this module returns <c>ATTACH_FILE</c> bytes. Never used for the list endpoint.
/// </summary>
public sealed record PettyCashAttachmentFileDto(
    Guid Id,
    string AttachName,
    string? ContentType,
    byte[] Content);
