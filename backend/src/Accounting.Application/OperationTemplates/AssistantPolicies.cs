using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Periods;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.OperationTemplates;

/// <summary>
/// دو قاعدهٔ حسابیار که به واحد کاربر بسته‌اند:
/// <list type="bullet">
/// <item><b>نوع واحد:</b> الگویی که <see cref="OperationTemplate.AllowedVahedTypes"/> دارد فقط برای واحدهای همان
/// نوع‌ها دیده و اجرا می‌شود (DDL 071).</item>
/// <item><b>قفل دوره:</b> اگر دورهٔ سال مالی واحد (یا واحد بالادستی‌اش) در «بستن دوره» قفل شده باشد، حسابیار سند
/// نمی‌سازد. ⚠️ فقط حسابیار — تصمیم قبلی صاحب پروژه: قفل دوره ثبت سند فرم معمولی را محدود نمی‌کند.</item>
/// </list>
/// </summary>
public sealed class AssistantUnitPolicy
{
    private readonly IUnitAccessReadRepository _units;
    private readonly FsPeriodGuard _periods;
    private IReadOnlyList<UnitNode>? _allUnits;

    public AssistantUnitPolicy(IUnitAccessReadRepository units, FsPeriodGuard periods)
    {
        _units = units;
        _periods = periods;
    }

    /// <summary>کد نوع واحد (<c>TB_VAHEDTYPE.TYPECODE</c>)؛ null اگر معلوم نشد.</summary>
    public async Task<string?> UnitTypeAsync(string vahedCode, CancellationToken ct)
    {
        _allUnits ??= await _units.GetAllUnitsAsync(ct);
        return _allUnits.FirstOrDefault(u => u.VahedCode == vahedCode)?.TypeCode;
    }

    public static bool IsAllowed(OperationTemplate t, string? unitType)
    {
        var allowed = TemplateMapping.SplitVahedTypes(t.AllowedVahedTypes);
        return allowed.Count == 0 || (unitType is not null && allowed.Contains(unitType));
    }

    public async Task<IReadOnlyList<OperationTemplate>> FilterAsync(IReadOnlyList<OperationTemplate> templates, string vahedCode, CancellationToken ct)
    {
        if (templates.All(t => string.IsNullOrWhiteSpace(t.AllowedVahedTypes)))
            return templates;
        var type = await UnitTypeAsync(vahedCode, ct);
        return templates.Where(t => IsAllowed(t, type)).ToList();
    }

    public async Task<EngineError?> TemplateNotAllowedAsync(OperationTemplate t, string vahedCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(t.AllowedVahedTypes))
            return null;
        return IsAllowed(t, await UnitTypeAsync(vahedCode, ct))
            ? null
            : new EngineError(EngineErrorCode.TemplateInactive, $"الگوی «{t.Title}» برای نوع واحد شما تعریف نشده است.");
    }

    /// <summary>
    /// خطای «دوره قفل است» (کلید <c>voucherDate</c> تا همان سؤال تاریخ دوباره پرسیده شود)، یا null.
    /// اگر جدول دوره‌ها (DDL 062) در دسترس نباشد، مانع ثبت نمی‌شود.
    /// </summary>
    public async Task<EngineError?> PeriodLockedAsync(string vahedCode, DateOnly voucherDate, CancellationToken ct)
    {
        var year = FiscalYearGuard.JalaliYear(voucherDate);
        try
        {
            var unlocked = await _periods.GetUnlockedAsync(vahedCode, includeSubUnits: false, year, ct);
            if (unlocked.Count > 0)
                return null;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return null;
        }
        return new EngineError(EngineErrorCode.InvalidParameterValue,
            $"دورهٔ سال مالی {year} برای واحد شما قفل شده است («بستن دوره»)؛ حسابیار در این دوره سند ثبت نمی‌کند. تاریخ دیگری انتخاب کنید یا بازگشایی دوره را درخواست کنید.",
            "voucherDate", "سند به چه تاریخی ثبت شود؟");
    }
}
