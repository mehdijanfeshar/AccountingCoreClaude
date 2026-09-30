using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Queries;

/// <summary>خلاصهٔ یک نسخهٔ قالب در فهرست قالب‌ها.</summary>
public sealed record FsTemplateVersionSummaryDto(
    Guid Id,
    int VersionNo,
    FsTemplateVersionState State,
    int? EffectiveFromYear,
    string? Description,
    string? ActivatedBy,
    DateTime? ActivatedDate,
    int RowCount,
    DateTime CreatedDate);

/// <summary>
/// <c>GET api/fs/templates</c> — یک قالب با همهٔ نسخه‌های حذف‌نشده‌اش (جدیدترین اول).
/// <paramref name="OwnerVahedCode"/> خالی = قالب مشترک؛ <paramref name="CanEdit"/> برای واحد هدر.
/// </summary>
public sealed record FsTemplateDto(
    Guid Id,
    string? OwnerVahedCode,
    string? OwnerVahedName,
    bool CanEdit,
    FsFramework Framework,
    string Code,
    string TitleFa,
    string? TitleEn,
    FsStatementType StatementType,
    int OrderNo,
    string? NoteParentTemplateCode,
    string? NoteParentRowCode,
    string? NoteTotalRowCode,
    IReadOnlyList<FsTemplateVersionSummaryDto> Versions);

/// <summary>یک ردیف قالب. <paramref name="ParentCode"/> نمایشی است (کد ردیف والد).</summary>
public sealed record FsTemplateRowDto(
    Guid Id,
    string Code,
    Guid? ParentId,
    string? ParentCode,
    int OrderNo,
    FsRowType RowType,
    string? TitleFa,
    string? TitleEn,
    string? NoteRef,
    FsNormalBalance? NormalBalance,
    string? Selector,
    FsValueType? ValueType,
    string? Formula,
    FsRowFormat Format,
    bool IsDrillable,
    bool AllowManualAdjust);

/// <summary><c>GET api/fs/template-versions/{id}</c> — نسخه با همهٔ ردیف‌ها به ترتیب ارائه.</summary>
public sealed record FsTemplateVersionDetailDto(
    Guid Id,
    Guid TemplateId,
    string? OwnerVahedCode,
    bool CanEdit,
    string TemplateCode,
    string TemplateTitleFa,
    FsFramework Framework,
    FsStatementType StatementType,
    string? NoteParentTemplateCode,
    string? NoteParentRowCode,
    string? NoteTotalRowCode,
    int VersionNo,
    FsTemplateVersionState State,
    int? EffectiveFromYear,
    string? Description,
    string? ActivatedBy,
    DateTime? ActivatedDate,
    string? ContentHash,
    IReadOnlyList<FsTemplateRowDto> Rows);

/// <summary><c>POST api/fs/template-versions/{id}/validate</c>. <paramref name="IsValid"/> = هیچ خطای (نه هشدار) ندارد.</summary>
public sealed record FsTemplateCheckResultDto(bool IsValid, IReadOnlyList<FsTemplateIssue> Issues);
