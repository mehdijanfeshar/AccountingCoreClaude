using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.ValidateFsTemplateVersion;

/// <summary>
/// <c>POST api/fs/template-versions/{id}/validate</c> — همان بررسی فعال‌سازی، بدون تغییر چیزی؛ برای
/// هر وضعیتی از نسخه قابل اجراست. <see langword="null"/> = ۴۰۴.
/// </summary>
public sealed record ValidateFsTemplateVersionQuery(Guid Id) : IRequest<FsTemplateCheckResultDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
