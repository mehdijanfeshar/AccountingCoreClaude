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
    DateTime CreatedDate,
    bool PriorRestated = false,
    bool IncludeEntities = false);

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
    IReadOnlyList<FsRunStatementDto> Statements,
    Guid? SourceRunId,
    IReadOnlyList<FsRunCheckDto> Checks,
    IReadOnlyList<FsRunActionDto> Actions,
    IReadOnlyList<FsRunManualDto> ManualValues,
    IReadOnlyList<Approvals.FsRunApprovalStepDto>? ApprovalSteps = null,
    IReadOnlyList<string>? UnlockedUnits = null);

/// <summary>نتیجهٔ یک کنترل در اجرا (بخش ۴۵-ه).</summary>
public sealed record FsRunCheckDto(
    string Code,
    string TitleFa,
    FsCheckSeverity Severity,
    bool Passed,
    string? Message,
    decimal? Difference,
    string? RowRef,
    Guid? Id = null,
    string? AssigneeUserId = null,
    string? AssigneeName = null,
    string? DueDate = null,
    FsCheckAssignState? AssignState = null,
    string? AssignedBy = null);

/// <summary>ح-۳ — یک «نظر» روی ردیف، کنترل یا کل اجرا. <paramref name="IsMine"/> = کاربر جاری نوشته (قابل حذف).</summary>
public sealed record FsRunCommentDto(Guid Id, Guid? RowId, Guid? CheckId, string Body, string UserId, DateTime CreatedDate, bool IsMine);

/// <summary>یک قدم گردش تأیید.</summary>
public sealed record FsRunActionDto(
    FsRunAction Action,
    FsRunState FromState,
    FsRunState ToState,
    string UserId,
    string? Comments,
    DateTime CreatedDate,
    int? StepNo = null);

/// <summary>مقدار دستی یک ردیف «مقدار دستی» — مبلغ به علامت نمایشی.</summary>
public sealed record FsRunManualDto(
    string TemplateCode,
    string RowCode,
    decimal? AmountCur,
    decimal? AmountPrv,
    string Reason,
    string AddUserId,
    DateTime CreatedDate);

/// <summary><c>GET api/fs/runs/{a}/diff/{b}</c> — یک ردیف در مقایسهٔ دو اجرا (مبالغ به علامت حسابداری).</summary>
public sealed record FsRunDiffRowDto(
    string TemplateCode,
    string StatementTitle,
    bool IsNote,
    string RowCode,
    string? TitleFa,
    FsNormalBalance? NormalBalance,
    decimal? AmountA,
    decimal? AmountB);

/// <summary><c>GET api/fs/runs/{id}/staleness</c> — آیا مانده‌های منبع پس از اجرا عوض شده‌اند.</summary>
public sealed record FsRunStalenessDto(bool IsStale, bool Unknown);

/// <summary><c>GET api/fs/check-rules</c> — قاعدهٔ کنترل تساوی بین صورت‌ها.</summary>
public sealed record FsCheckRuleDto(
    Guid Id,
    string? OwnerVahedCode,
    bool CanEdit,
    FsFramework Framework,
    string Code,
    string TitleFa,
    string LeftExpr,
    string RightExpr,
    decimal Tolerance,
    FsCheckSeverity Severity,
    bool IsActive);

/// <summary>ح-۵ — آخرین اجرای یک واحد در یک سال: وضعیت و تعداد کنترل مسدودکنندهٔ ناموفق.</summary>
public sealed record FsUnitRunStatusDto(
    string VahedCode,
    Guid RunId,
    int RunNo,
    FsFramework Framework,
    FsRunState State,
    DateTime CreatedDate,
    int BlockingFailed);

/// <summary>ح-۹ — یک کنترل ارجاع‌شدهٔ باز (برای «کارهای من»).</summary>
public sealed record FsAssignedCheckDto(Guid RunId, int RunNo, Guid CheckId, string Code, string TitleFa, string? DueDate, string? AssignedBy);
