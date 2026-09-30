using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Commands.Common;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.ImportFsTemplateRows;

/// <summary>
/// <c>POST api/fs/template-versions/{versionId}/rows/import</c> — <b>جایگزینی کامل</b> ردیف‌های
/// نسخهٔ پیش‌نویس با فهرست داده‌شده (هم‌شکل پیوست الف سند منبع). اتمیک: یا همه یا هیچ.
/// پاسخ = تعداد ردیف‌ها.
/// </summary>
public sealed record ImportFsTemplateRowsCommand(Guid VersionId, IReadOnlyList<FsTemplateRowInput> Rows) : IRequest<int>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
