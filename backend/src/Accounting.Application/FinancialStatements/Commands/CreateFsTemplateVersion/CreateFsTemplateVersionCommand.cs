using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.CreateFsTemplateVersion;

/// <summary>
/// <c>POST api/fs/templates/{templateId}/versions</c> — نسخهٔ پیش‌نویس تازه با کپی ردیف‌های
/// <paramref name="SourceVersionId"/> (یا آخرین نسخهٔ قالب اگر خالی باشد). هر قالب حداکثر یک
/// پیش‌نویس باز دارد (۴۰۹). پاسخ = شناسهٔ نسخهٔ جدید.
/// </summary>
public sealed record CreateFsTemplateVersionCommand(Guid TemplateId, Guid? SourceVersionId, string? Description) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
