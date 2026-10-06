using System.Text;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.OperationTemplates;

// IUnitContext طرح اولیه حذف شد: واحد از IVahedScoped (VahedScopeBehavior + IUnitScopeResolver)
// و کاربر از ICurrentUser می‌آید — همان مسیر بقیهٔ پروژه.

public interface IOperationTemplateRepository
{
    /// <summary>الگو همراه Parameters، Lines و Lines.Details</summary>
    Task<OperationTemplate?> GetFullAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<OperationTemplate>> ListActiveWithParametersAsync(CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);
    /// <summary>کد را الگوی دیگری (غیر از <paramref name="exceptId"/>) دارد؟</summary>
    Task<bool> CodeExistsForOtherAsync(string code, Guid? exceptId, CancellationToken ct);
    Task AddAsync(OperationTemplate template, CancellationToken ct);

    /// <summary>همهٔ الگوها (فعال و غیرفعال) با پارامترها و ردیف‌ها، برای صفحهٔ طراحی.</summary>
    Task<IReadOnlyList<OperationTemplate>> ListAllAsync(CancellationToken ct);
    /// <summary>الگوی tracked با فرزندان، برای ویرایش.</summary>
    Task<OperationTemplate?> GetForUpdateAsync(Guid id, CancellationToken ct);
    /// <summary>حذف پارامترها، ردیف‌ها و تفصیلی ردیف‌های یک الگوی tracked (پیش از جایگزینی).</summary>
    void RemoveChildren(OperationTemplate template);
    /// <summary>
    /// پارامترها، ردیف‌ها و تفصیلی ردیف‌های جدید را صریحاً Added می‌کند. فقط انتساب مجموعه روی الگوی
    /// tracked کافی نیست: EF فرزندِ دارای کلید را «موجود» فرض می‌کند و UPDATE می‌زند.
    /// </summary>
    void AddChildren(OperationTemplate template);

    Task<OperationExecution?> FindExecutionAsync(Guid clientRequestId, CancellationToken ct);
    Task AddExecutionAsync(OperationExecution execution, CancellationToken ct);

    /// <summary>ردپای اجراها، جدیدترین اول (صفحهٔ «سندهای حسابیار» و «عملیات‌های اخیر من»).</summary>
    Task<(IReadOnlyList<OperationExecution> Items, int Total)> ListExecutionsAsync(ExecutionFilter filter, CancellationToken ct);

    /// <summary>تعداد و آخرین استفادهٔ هر الگو؛ <paramref name="vahedCode"/> null = همهٔ واحدها.</summary>
    Task<IReadOnlyList<TemplateUsageRow>> UsageAsync(string? vahedCode, CancellationToken ct);
}

/// <param name="VahedCode">null = همهٔ واحدها (فقط برای مدیر ستاد).</param>
public sealed record ExecutionFilter(
    string? VahedCode, string? CreatedBy, Guid? TemplateId, string? Channel,
    DateTime? FromUtc, DateTime? ToUtc, int Skip, int Take);

public sealed record TemplateUsageRow(Guid TemplateId, int Count, DateTime LastUsedUtc);

/// <summary>
/// تبدیل ورودی کاربر/فرانت به قالب استاندارد موتور.
/// موتور عمداً سخت‌گیر است؛ هر نرمال‌سازی فقط همین‌جا انجام می‌شود.
/// </summary>
/// <summary>
/// تاریخ سند الگو: اگر الگو سؤال تاریخ دارد (اولین پارامتر نوع تاریخ، معمولاً <c>{date1}</c>)، جواب همان
/// سؤال تاریخ سند است (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۶)؛ وگرنه تاریخ ارسالی، وگرنه امروز.
/// </summary>
public static class TemplateVoucherDate
{
    public static TemplateParameter? DateParameter(OperationTemplate t) =>
        t.Parameters.Where(p => p.Type == ParameterType.Date).OrderBy(p => p.SortOrder).FirstOrDefault();

    public static DateOnly Resolve(OperationTemplate t, IReadOnlyDictionary<string, string?> normalizedValues, DateOnly? requested, DateOnly today)
    {
        if (DateParameter(t) is { } p
            && normalizedValues.TryGetValue(p.Key, out var raw)
            && DateOnly.TryParseExact(raw, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d))
            return d;
        return requested ?? today;
    }
}

public static class InputNormalizer
{
    public static string? Normalize(string? raw, ParameterType type)
    {
        if (raw is null) return null;
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw.Trim())
        {
            if (ch is >= '۰' and <= '۹') sb.Append((char)('0' + ch - '۰'));      // ۰-۹ فارسی
            else if (ch is >= '٠' and <= '٩') sb.Append((char)('0' + ch - '٠')); // ٠-٩ عربی
            else if (type == ParameterType.Amount && ch is ',' or '٬' or '،' or ' ' or '‌') { /* جداکننده هزارگان */ }
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    public static Dictionary<string, string?> NormalizeAll(
        OperationTemplate t, IReadOnlyDictionary<string, string?>? values)
    {
        var types = t.Parameters.ToDictionary(p => p.Key, p => p.Type, StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (values is null) return result;
        foreach (var (k, v) in values)
            result[k] = types.TryGetValue(k, out var ty) ? Normalize(v, ty) : v;
        return result;
    }
}
