using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Commands.ImportBankStatement;

/// <summary>
/// <c>POST api/treasury/statements/{id}/import</c> (multipart/form-data) — parses an uploaded bank
/// "disk" (دیسکت) statement file via <c>IBankStatementFileParser</c> and appends its lines as
/// <see cref="Accounting.Domain.ValueObjects.BankStatementLineMatchState.Unmatched"/>. <b>409</b>
/// (<c>TreasuryBankStatementParserNotConfiguredException</c>) as long as no parser implementation
/// is registered — owner decision ۲۰۲۶-۰۹-۲۹, خزانه‌داری بخش ۴-د.
///
/// <c>Content</c> is <see langword="byte"/>[], not <c>IFormFile</c>/<c>Stream</c> — same
/// <c>Accounting.Application</c>-takes-no-ASP.NET-Core-dependency boundary as
/// <c>UploadPettyCashAttachmentCommand</c>.
/// </summary>
public sealed record ImportBankStatementCommand(
    Guid StatementId, byte[] Content) : IRequest<int>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
