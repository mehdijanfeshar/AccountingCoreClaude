using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Queries;

/// <summary><c>GET api/fs/runs</c> — یک اجرا در فهرست.</summary>
public sealed record FsRunSummaryDto(
    Guid Id,
    int RunNo,
    string VahedCode,
    string? VahedName,
    bool IncludeSubUnits,
    int UnitCount,
    FsFramework Framework,
    string Year,
    int ToMonth,
    int MinDocLife,
    bool HasPrior,
    bool UsesDraft,
    FsRunState State,
    string? Description,
    int StatementCount,
    int? DurationMs,
    string AddUserId,
    DateTime CreatedDate);

/// <summary>
/// یک ردیف صورت در Snapshot. <paramref name="AmountCur"/>/<paramref name="AmountPrv"/> با علامت حسابداری
/// (بدهکار مثبت) — نمایش با <paramref name="NormalBalance"/> بستانکار قرینه می‌کند.
/// </summary>
public sealed record FsRunRowDto(
    Guid Id,
    string Code,
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
    decimal? AmountCur,
    decimal? AmountPrv);

/// <summary>
/// یک صورت یا یادداشت اجرا. برای یادداشت: <paramref name="NoteNo"/> شمارهٔ داده‌شده، والد، و اختلاف کنترل V-08
/// (جمع یادداشت − ردیف صورت؛ صفر = برابر، <see langword="null"/> = بی‌والد).
/// </summary>
public sealed record FsRunStatementDto(
    Guid Id,
    Guid TemplateId,
    Guid VersionId,
    string TemplateCode,
    string TitleFa,
    FsStatementType StatementType,
    int OrderNo,
    int VersionNo,
    FsTemplateVersionState VersionState,
    bool IsNote,
    string? NoteNo,
    string? ParentTemplateCode,
    string? ParentRowCode,
    string? TotalRowCode,
    decimal? CheckDiffCur,
    decimal? CheckDiffPrv,
    IReadOnlyList<FsRunRowDto> Rows);

/// <summary><c>GET api/fs/runs/{id}</c> — اجرا با همهٔ صورت‌ها و ردیف‌ها (از Snapshot).</summary>
public sealed record FsRunDetailDto(
    FsRunSummaryDto Run,
    string FromDate,
    string ToDate,
    string? ContentHash,
    int NoteStartNo,
    IReadOnlyList<FsRunStatementDto> Statements);
