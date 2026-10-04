using Accounting.Domain.ValueObjects;
using Accounting.Application.FinancialStatements.Access;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.UpdateFsTemplate;

/// <summary>
/// <c>POST api/fs/templates/{id}/update</c> — فقط عنوان‌ها و ترتیب. کد، مجموعه و نوع صورت
/// تغییرناپذیرند (فرمول‌های صورت‌های دیگر با کد به این قالب ارجاع می‌دهند).
/// برای یادداشت، ارتباط با ردیف صورت و ردیف جمع هم عوض می‌شود (اثرش فقط روی اجراهای بعدی).
/// </summary>
[FsRequires(FsOperation.EditTemplate)]
public sealed record UpdateFsTemplateCommand(
    Guid Id,
    string TitleFa,
    string? TitleEn,
    int OrderNo,
    string? NoteParentTemplateCode = null,
    string? NoteParentRowCode = null,
    string? NoteTotalRowCode = null) : IRequest, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
