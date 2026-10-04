using Accounting.Application.FinancialStatements.Access;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.CreateFsTemplate;

/// <summary>
/// <c>POST api/fs/templates</c> — قالب صورت جدید + نسخهٔ پیش‌نویس ۱ (خالی). <paramref name="Code"/>
/// در هر مالک یکتا و تغییرناپذیر است (مثل <c>PENSION.NET_ASSETS</c>) و پس از حذف قالب دوباره آزاد نمی‌شود.
/// <paramref name="Shared"/> = قالب مشترک همهٔ واحدها (فقط ستاد، وگرنه ۴۰۳)؛ در غیر این صورت قالب
/// اختصاصی واحد هدر که برای آن واحد و زیرمجموعه‌هایش بر قالب مشترکِ هم‌کد مقدم است.
/// سه فیلد <c>Note*</c> فقط برای <see cref="FsStatementType.Note"/> (بخش ۴۵-ج): صورت و ردیفی که یادداشت
/// به آن وصل است، و ردیف جمع یادداشت؛ برای بقیه نادیده گرفته می‌شوند.
/// </summary>
[FsRequires(FsOperation.EditTemplate)]
public sealed record CreateFsTemplateCommand(
    FsFramework Framework,
    string Code,
    string TitleFa,
    string? TitleEn,
    FsStatementType StatementType,
    int OrderNo,
    bool Shared = false,
    string? NoteParentTemplateCode = null,
    string? NoteParentRowCode = null,
    string? NoteTotalRowCode = null) : IRequest<CreateFsTemplateResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record CreateFsTemplateResult(Guid TemplateId, Guid VersionId);
