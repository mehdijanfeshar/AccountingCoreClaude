using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.DeleteFsTemplateVersion;

/// <summary>
/// <c>POST api/fs/template-versions/{id}/delete</c> — حذف نرم؛ فقط نسخهٔ پیش‌نویس. نسخهٔ فعال یا
/// بازنشسته سابقهٔ اجراهای گذشته است و هرگز حذف نمی‌شود (۴۰۹).
/// </summary>
public sealed record DeleteFsTemplateVersionCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
