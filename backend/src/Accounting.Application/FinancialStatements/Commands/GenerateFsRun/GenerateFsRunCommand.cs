using Accounting.Application.FinancialStatements.Access;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Commands.GenerateFsRun;

/// <summary>
/// <c>POST api/fs/runs</c> — تهیهٔ صورت‌های مالی یک مجموعه برای واحد هدر (<c>X-Vahed-Code</c>) و
/// دورهٔ «ابتدای <paramref name="Year"/> تا پایان ماه <paramref name="ToMonth"/>»؛ نتیجه Snapshot
/// تغییرناپذیر است. اجرا هم‌زمان است (<c>docs/fs-module.md</c> §۱). پاسخ = شناسهٔ اجرا.
/// </summary>
/// <param name="IncludeSubUnits">صورت ترکیبی: اسناد همهٔ واحدهای زیرمجموعه (برای ستاد = همهٔ واحدها) هم جمع شود.</param>
/// <param name="MinDocLife">کمینهٔ وضعیت سند (۱ یادداشت … ۴ تأیید دائم)؛ پیش‌فرض فرانت ۴.</param>
/// <param name="IncludePrior">ستون همان دوره در سال قبل هم محاسبه شود.</param>
/// <param name="UseDraftVersions">پیش‌نویس باز هر قالب بر نسخهٔ فعال مقدم باشد (اجرای «آزمایشی»).</param>
/// <param name="SourceRunId">بخش ۴۵-ه — اجرای پیش‌نویسی که این اجرا جایگزینش می‌شود (مثلاً پس از ورود مقادیر دستی)؛ آن اجرا «جایگزین‌شده» می‌شود.</param>
/// <param name="ManualValues">بخش ۴۵-ه — مقدار ردیف‌های «مقدار دستی»، به علامت نمایشی، هرکدام با دلیل.</param>
/// <param name="IncludeEntities">ط-۵ — تلفیق با شرکت‌های تابعهٔ فعالِ واحد (تراز واردشده از Excel).</param>
/// <param name="PriorRestated">ستون سال قبل با برچسب «تجدید ارائه‌شده» (ح-۲؛ فقط برچسب).</param>
[FsRequires(FsOperation.Prepare)]
public sealed record GenerateFsRunCommand(
    FsFramework Framework,
    string Year,
    int ToMonth,
    bool IncludeSubUnits,
    int MinDocLife,
    bool IncludePrior,
    bool UseDraftVersions,
    string? Description,
    int NoteStartNo = 1,
    Guid? SourceRunId = null,
    IReadOnlyList<FsManualValueInput>? ManualValues = null,
    bool PriorRestated = false,
    bool IncludeEntities = false) : IRequest<Guid>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>مقدار دستی یک ردیف «مقدار دستی» (External) — مبلغ به علامت نمایشی، با دلیل (بخش ۴۵-ه).</summary>
public sealed record FsManualValueInput(string TemplateCode, string RowCode, decimal? AmountCur, decimal? AmountPrv, string Reason);
