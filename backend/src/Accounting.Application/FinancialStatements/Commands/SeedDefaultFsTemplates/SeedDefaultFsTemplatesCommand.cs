using Accounting.Domain.ValueObjects;
using Accounting.Application.FinancialStatements.Access;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.SeedDefaultFsTemplates;

/// <summary>
/// <c>POST api/fs/templates/seed-defaults</c> — قالب‌های پیش‌فرض (<see cref="FsDefaultTemplates"/>) را
/// می‌سازد، هرکدام با نسخهٔ پیش‌نویس ۱. قالبی که کدش از قبل وجود دارد (حتی حذف‌شده) نادیده گرفته
/// می‌شود، پس تکرار فراخوانی بی‌خطر است. پاسخ = کد قالب‌های ساخته‌شده.
/// </summary>
[FsRequires(FsOperation.EditTemplate)]
public sealed record SeedDefaultFsTemplatesCommand : IRequest<IReadOnlyList<string>>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
