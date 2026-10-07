using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using MediatR;

namespace Accounting.Application.OperationTemplates;

// ═══════════════════════ گزارش‌ساز حسابیار — گزارش‌های ذخیره‌شده (DDL 072) ═══════════════════════
// فقط تعریف: کدام گزارش موجود + تنظیمات ثابت + پارامترهای پرسیدنی. عددها را همان گزارش‌های فعلی
// (تراز، ماتریسی، مرور حساب‌ها، مرور اسناد) با دسترسی واحد خودشان می‌سازند. دیدن برای همه، تغییر فقط مدیر ستاد.

public interface ISavedReportRepository
{
    Task<IReadOnlyList<SavedReport>> ListAsync(bool activeOnly, CancellationToken ct);
    Task<SavedReport?> GetAsync(Guid id, CancellationToken ct);
    Task<SavedReport?> GetForUpdateAsync(Guid id, CancellationToken ct);
    Task<bool> CodeExistsForOtherAsync(string code, Guid? exceptId, CancellationToken ct);
    Task AddAsync(SavedReport report, CancellationToken ct);
}

public sealed record SavedReportDto(
    Guid Id, string Code, string Title, string? Description, string? Keywords, string ReportKind,
    string SettingsJson, IReadOnlyList<string> AllowedVahedTypes, bool IsActive);

public sealed record SavedReportWriteResult(bool Success, Guid? Id, IReadOnlyList<string> Errors);

internal static class SavedReportRules
{
    public static readonly string[] Kinds = ["trial-balance", "matrix", "account-review", "voucher-review"];
    private static readonly Regex CodePattern = new("^[A-Z0-9_]{2,50}$", RegexOptions.Compiled);

    public static SavedReportDto ToDto(SavedReport r) => new(r.Id, r.Code, r.Title, r.Description, r.Keywords, r.ReportKind,
        r.SettingsJson, TemplateMapping.SplitVahedTypes(r.AllowedVahedTypes), r.IsActive);

    public static List<string> Validate(string code, string title, string? description, string? keywords, string kind, string settingsJson, string? vahedTypes)
    {
        var e = new List<string>();
        if (!CodePattern.IsMatch(code)) e.Add("کد گزارش باید ۲ تا ۵۰ حرف لاتین بزرگ، رقم یا «_» باشد.");
        if (title.Length is 0 or > 200) e.Add("عنوان گزارش الزامی است و حداکثر ۲۰۰ کاراکتر.");
        if ((description?.Length ?? 0) > 1000) e.Add("توضیح حداکثر ۱۰۰۰ کاراکتر است.");
        if ((keywords?.Length ?? 0) > 1000) e.Add("کلمات کلیدی حداکثر ۱۰۰۰ کاراکتر است.");
        if (!Kinds.Contains(kind)) e.Add("نوع گزارش نامعتبر است.");
        if ((vahedTypes?.Length ?? 0) > 400) e.Add("فهرست نوع واحدهای مجاز طولانی‌تر از حد است.");
        if (settingsJson.Length > 4000) e.Add("تنظیمات گزارش بیش از حد طولانی است.");
        else
        {
            try
            {
                using var doc = JsonDocument.Parse(settingsJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) e.Add("تنظیمات گزارش معتبر نیست.");
            }
            catch (JsonException)
            {
                e.Add("تنظیمات گزارش معتبر نیست.");
            }
        }
        return e;
    }
}

// ─────────── فهرست برای حسابیار (فعال + مجاز برای نوع واحد) ───────────

public sealed record ListSavedReportsQuery : IRequest<IReadOnlyList<SavedReportDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListSavedReportsHandler : IRequestHandler<ListSavedReportsQuery, IReadOnlyList<SavedReportDto>>
{
    private readonly ISavedReportRepository _repo;
    private readonly AssistantUnitPolicy _policy;

    public ListSavedReportsHandler(ISavedReportRepository repo, AssistantUnitPolicy policy) { _repo = repo; _policy = policy; }

    public async Task<IReadOnlyList<SavedReportDto>> Handle(ListSavedReportsQuery q, CancellationToken ct)
    {
        var list = await _repo.ListAsync(activeOnly: true, ct);
        var unitType = list.Any(r => !string.IsNullOrWhiteSpace(r.AllowedVahedTypes)) ? await _policy.UnitTypeAsync(q.VahedCode, ct) : null;
        return list
            .Where(r => TemplateMapping.SplitVahedTypes(r.AllowedVahedTypes) is var allowed && (allowed.Count == 0 || (unitType is not null && allowed.Contains(unitType))))
            .Select(SavedReportRules.ToDto)
            .ToList();
    }
}

// ─────────── همهٔ تعریف‌ها (صفحهٔ مدیریت) و یکی ───────────

public sealed record ListSavedReportDefinitionsQuery : IRequest<IReadOnlyList<SavedReportDto>>;

public sealed class ListSavedReportDefinitionsHandler : IRequestHandler<ListSavedReportDefinitionsQuery, IReadOnlyList<SavedReportDto>>
{
    private readonly ISavedReportRepository _repo;
    public ListSavedReportDefinitionsHandler(ISavedReportRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SavedReportDto>> Handle(ListSavedReportDefinitionsQuery q, CancellationToken ct) =>
        (await _repo.ListAsync(activeOnly: false, ct)).Select(SavedReportRules.ToDto).ToList();
}

public sealed record GetSavedReportQuery(Guid Id) : IRequest<SavedReportDto>;

public sealed class GetSavedReportHandler : IRequestHandler<GetSavedReportQuery, SavedReportDto>
{
    private readonly ISavedReportRepository _repo;
    public GetSavedReportHandler(ISavedReportRepository repo) => _repo = repo;

    public async Task<SavedReportDto> Handle(GetSavedReportQuery q, CancellationToken ct) =>
        SavedReportRules.ToDto(await _repo.GetAsync(q.Id, ct) ?? throw new NotFoundException("SavedReport", q.Id));
}

// ─────────── ایجاد / ویرایش / فعال‌سازی — فقط مدیر ستاد ───────────

public sealed record CreateSavedReportCommand(
    string Code, string Title, string? Description, string? Keywords, string ReportKind, string SettingsJson,
    IReadOnlyList<string>? AllowedVahedTypes = null) : IRequest<SavedReportWriteResult>;

public sealed class CreateSavedReportHandler : IRequestHandler<CreateSavedReportCommand, SavedReportWriteResult>
{
    private readonly ISavedReportRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    private readonly IHeadquartersAccessService? _hq;


    public CreateSavedReportHandler(ISavedReportRepository repo, IUnitOfWork unitOfWork, ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    { _repo = repo; _unitOfWork = unitOfWork; _currentUser = currentUser; _hq = hq; }

    public async Task<SavedReportWriteResult> Handle(CreateSavedReportCommand c, CancellationToken ct)
    {
        await TemplateMapping.EnsureAbilityAsync(_currentUser, _hq, AbilityCatalog.SavedReportsDefine, "گزارش ذخیره‌شده", ct);
        var code = c.Code?.Trim().ToUpperInvariant() ?? "";
        var vahedTypes = TemplateMapping.JoinVahedTypes(c.AllowedVahedTypes);
        var errors = SavedReportRules.Validate(code, c.Title?.Trim() ?? "", c.Description, c.Keywords, c.ReportKind ?? "", c.SettingsJson ?? "", vahedTypes);
        if (errors.Count == 0 && await _repo.CodeExistsForOtherAsync(code, null, ct))
            errors.Add($"کد گزارش «{code}» قبلاً ثبت شده است.");
        if (errors.Count > 0) return new(false, null, errors);

        var r = new SavedReport
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = c.Title!.Trim(),
            Description = string.IsNullOrWhiteSpace(c.Description) ? null : c.Description.Trim(),
            Keywords = string.IsNullOrWhiteSpace(c.Keywords) ? null : c.Keywords.Trim(),
            ReportKind = c.ReportKind,
            SettingsJson = c.SettingsJson,
            AllowedVahedTypes = vahedTypes,
            IsActive = true,
            CreatedBy = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _repo.AddAsync(r, ct);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            return new(false, null, [$"کد گزارش «{code}» قبلاً ثبت شده است."]);
        }
        return new(true, r.Id, []);
    }
}

public sealed record UpdateSavedReportCommand(
    string Code, string Title, string? Description, string? Keywords, string ReportKind, string SettingsJson,
    IReadOnlyList<string>? AllowedVahedTypes = null) : IRequest<SavedReportWriteResult>
{
    [JsonIgnore]
    public Guid Id { get; set; }
}

public sealed class UpdateSavedReportHandler : IRequestHandler<UpdateSavedReportCommand, SavedReportWriteResult>
{
    private readonly ISavedReportRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    private readonly IHeadquartersAccessService? _hq;


    public UpdateSavedReportHandler(ISavedReportRepository repo, IUnitOfWork unitOfWork, ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    { _repo = repo; _unitOfWork = unitOfWork; _currentUser = currentUser; _hq = hq; }

    public async Task<SavedReportWriteResult> Handle(UpdateSavedReportCommand c, CancellationToken ct)
    {
        await TemplateMapping.EnsureAbilityAsync(_currentUser, _hq, AbilityCatalog.SavedReportsDefine, "گزارش ذخیره‌شده", ct);
        var code = c.Code?.Trim().ToUpperInvariant() ?? "";
        var vahedTypes = TemplateMapping.JoinVahedTypes(c.AllowedVahedTypes);
        var errors = SavedReportRules.Validate(code, c.Title?.Trim() ?? "", c.Description, c.Keywords, c.ReportKind ?? "", c.SettingsJson ?? "", vahedTypes);
        if (errors.Count == 0 && await _repo.CodeExistsForOtherAsync(code, c.Id, ct))
            errors.Add($"کد گزارش «{code}» قبلاً ثبت شده است.");
        if (errors.Count > 0) return new(false, c.Id, errors);

        var r = await _repo.GetForUpdateAsync(c.Id, ct) ?? throw new NotFoundException("SavedReport", c.Id);
        r.Code = code;
        r.Title = c.Title!.Trim();
        r.Description = string.IsNullOrWhiteSpace(c.Description) ? null : c.Description.Trim();
        r.Keywords = string.IsNullOrWhiteSpace(c.Keywords) ? null : c.Keywords.Trim();
        r.ReportKind = c.ReportKind;
        r.SettingsJson = c.SettingsJson;
        r.AllowedVahedTypes = vahedTypes;
        await _unitOfWork.SaveChangesAsync(ct);
        return new(true, r.Id, []);
    }
}

public sealed record SetSavedReportActiveCommand(bool IsActive) : IRequest<Unit>
{
    [JsonIgnore]
    public Guid Id { get; set; }
}

public sealed class SetSavedReportActiveHandler : IRequestHandler<SetSavedReportActiveCommand, Unit>
{
    private readonly ISavedReportRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    private readonly IHeadquartersAccessService? _hq;


    public SetSavedReportActiveHandler(ISavedReportRepository repo, IUnitOfWork unitOfWork, ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    { _repo = repo; _unitOfWork = unitOfWork; _currentUser = currentUser; _hq = hq; }

    public async Task<Unit> Handle(SetSavedReportActiveCommand c, CancellationToken ct)
    {
        await TemplateMapping.EnsureAbilityAsync(_currentUser, _hq, AbilityCatalog.SavedReportsDefine, "گزارش ذخیره‌شده", ct);
        var r = await _repo.GetForUpdateAsync(c.Id, ct) ?? throw new NotFoundException("SavedReport", c.Id);
        r.IsActive = c.IsActive;
        await _unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
