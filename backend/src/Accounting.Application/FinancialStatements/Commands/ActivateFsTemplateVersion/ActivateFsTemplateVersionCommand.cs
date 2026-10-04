using Accounting.Domain.ValueObjects;
using Accounting.Application.FinancialStatements.Access;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.ActivateFsTemplateVersion;

/// <summary>
/// <c>POST api/fs/template-versions/{id}/activate</c> — نسخهٔ پیش‌نویس را پس از اعتبارسنجی کامل
/// (<c>FsTemplateChecker</c>؛ هر خطا = ۴۰۰ با فهرست یافته‌ها) فعال و تغییرناپذیر می‌کند، از سال مالی
/// <paramref name="EffectiveFromYear"/> به بعد. نسخهٔ فعال دیگرِ همین قالب با <b>همان</b> سال شروع
/// بازنشسته می‌شود؛ نسخه‌های فعال سال‌های دیگر دست نمی‌خورند.
/// ⚠️ تفکیک وظایف «تغییر قالب ≠ فعال‌سازی» (سند منبع §۱۴) با RBAC ماژول در بخش ۴۵-د می‌آید.
/// </summary>
[FsRequires(FsOperation.ActivateTemplate)]
public sealed record ActivateFsTemplateVersionCommand(Guid Id, int EffectiveFromYear) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
