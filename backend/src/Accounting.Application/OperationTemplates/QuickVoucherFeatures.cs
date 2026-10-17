using System.Text.Json;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using MediatR;

namespace Accounting.Application.OperationTemplates;

// ═══════════════════════ سندهای پرتکرار حسابیار (DDL 079) ═══════════════════════
// فقط تعریف: الگو + جواب‌های ثابت برای واحد. ثبت از همان Preview/Execute الگو (موتور، قفل دوره، idempotency)؛
// هنگام ثبت فقط پارامترهای ثابت‌نشده پرسیده می‌شود و تاریخ سند امروز است. همهٔ کاربران واحد می‌بینند و می‌سازند.

public interface IQuickVoucherRepository
{
    Task<IReadOnlyList<QuickVoucher>> ListAsync(string vahedCode, CancellationToken ct);
    Task<QuickVoucher?> GetForUpdateAsync(Guid id, CancellationToken ct);
    Task AddAsync(QuickVoucher quick, CancellationToken ct);
}

/// <param name="FixedAnswers">جواب‌های ثابت (قالب «اخیرها»)؛ تفصیلی‌ای که دیگر فعال/قابل دید نیست حذف می‌شود و پرسیده می‌شود.</param>
public sealed record QuickVoucherDto(
    Guid Id, string Title, Guid TemplateId, string TemplateTitle, IReadOnlyList<RecentAnswerDto> FixedAnswers, string CreatedBy);

public sealed record QuickVoucherWriteResult(bool Success, Guid? Id, IReadOnlyList<string> Errors);

// ─────────── فهرست واحد جاری (فقط الگوهای فعال و مجاز برای نوع واحد) ───────────

public sealed record ListQuickVouchersQuery : IRequest<IReadOnlyList<QuickVoucherDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListQuickVouchersHandler : IRequestHandler<ListQuickVouchersQuery, IReadOnlyList<QuickVoucherDto>>
{
    private readonly IQuickVoucherRepository _repo;
    private readonly IOperationTemplateRepository _templates;
    private readonly IDetailReader _details;
    private readonly AssistantUnitPolicy _policy;

    public ListQuickVouchersHandler(IQuickVoucherRepository repo, IOperationTemplateRepository templates, IDetailReader details, AssistantUnitPolicy policy)
    { _repo = repo; _templates = templates; _details = details; _policy = policy; }

    public async Task<IReadOnlyList<QuickVoucherDto>> Handle(ListQuickVouchersQuery q, CancellationToken ct)
    {
        var list = await _repo.ListAsync(q.VahedCode, ct);
        if (list.Count == 0) return [];

        var templates = (await _policy.FilterAsync(await _templates.ListActiveWithParametersAsync(ct), q.VahedCode, ct))
            .ToDictionary(t => t.Id);
        var parsed = list
            .Where(x => templates.ContainsKey(x.TemplateId))
            .Select(x => (Quick: x, Template: templates[x.TemplateId], Values: ListMyRecentOperationsHandler.ParseValues(x.ValuesJson)))
            .ToList();
        var detailIds = parsed
            .SelectMany(p => p.Template.Parameters.Where(x => x.Type == ParameterType.Detail)
                .Select(x => p.Values.TryGetValue(x.Key, out var v) && Guid.TryParse(v, out var g) ? g : Guid.Empty))
            .Where(g => g != Guid.Empty).Distinct().ToList();
        var details = await _details.GetByIdsAsync(detailIds, q.VahedCode, ct);

        return parsed.Select(p => new QuickVoucherDto(
            p.Quick.Id, p.Quick.Title, p.Template.Id, p.Template.Title,
            p.Template.Parameters.Where(x => x.Type != ParameterType.Date).OrderBy(x => x.SortOrder)
                .Select(x => ListMyRecentOperationsHandler.ToAnswer(x, p.Values, details))
                .Where(a => a is not null)
                .Select(a => a!)
                .ToList(),
            p.Quick.CreatedBy)).ToList();
    }
}

// ─────────── ایجاد ───────────

/// <param name="Values">کلید پارامتر ⇒ مقدار ثابت (مبلغ ریال، تفصیلی Guid، متن). کلید نیامده/خالی هر بار پرسیده می‌شود؛ تاریخ پذیرفته نمی‌شود.</param>
public sealed record CreateQuickVoucherCommand(Guid TemplateId, string Title, Dictionary<string, string?>? Values)
    : IRequest<QuickVoucherWriteResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class CreateQuickVoucherHandler : IRequestHandler<CreateQuickVoucherCommand, QuickVoucherWriteResult>
{
    private readonly IQuickVoucherRepository _repo;
    private readonly IOperationTemplateRepository _templates;
    private readonly IDetailReader _details;
    private readonly AssistantUnitPolicy _policy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateQuickVoucherHandler(IQuickVoucherRepository repo, IOperationTemplateRepository templates, IDetailReader details,
        AssistantUnitPolicy policy, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _templates = templates; _details = details; _policy = policy; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<QuickVoucherWriteResult> Handle(CreateQuickVoucherCommand c, CancellationToken ct)
    {
        var errors = new List<string>();
        var title = c.Title?.Trim() ?? "";
        if (title.Length is 0 or > 200) errors.Add("عنوان سند پرتکرار الزامی است و حداکثر ۲۰۰ کاراکتر.");

        var template = await _templates.GetFullAsync(c.TemplateId, ct);
        if (template is null || !template.IsActive)
            return new(false, null, [.. errors, "الگوی عملیات یافت نشد یا غیرفعال است."]);
        if (await _policy.TemplateNotAllowedAsync(template, c.VahedCode, ct) is { } notAllowed)
            return new(false, null, [.. errors, notAllowed.Message]);

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var detailIds = new List<Guid>();
        foreach (var (key, raw) in c.Values ?? [])
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var p = template.Parameters.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            var value = raw.Trim();
            switch (p?.Type)
            {
                case null:
                    errors.Add($"«{key}» از سؤال‌های این الگو نیست.");
                    continue;
                case ParameterType.Date:
                    continue; // تاریخ سند پرتکرار همیشه روز ثبت است
                case ParameterType.Amount when !long.TryParse(value, out var amount) || amount <= 0:
                    errors.Add($"مبلغ «{p.Title}» باید عدد مثبت (ریال) باشد.");
                    continue;
                case ParameterType.Detail when !Guid.TryParse(value, out var id):
                    errors.Add($"«{p.Title}» معتبر نیست.");
                    continue;
                case ParameterType.Detail:
                    detailIds.Add(Guid.Parse(value));
                    break;
                case ParameterType.Text when value.Length > 500:
                    errors.Add($"«{p.Title}» حداکثر ۵۰۰ کاراکتر است.");
                    continue;
            }
            values[p.Key] = value;
        }

        if (detailIds.Count > 0)
        {
            var details = await _details.GetByIdsAsync(detailIds, c.VahedCode, ct);
            foreach (var id in detailIds.Where(id => !details.TryGetValue(id, out var d) || !d.IsActive || !d.IsVisibleToUnit))
                errors.Add($"تفصیلی انتخاب‌شده ({id}) برای این واحد فعال یا قابل استفاده نیست.");
        }

        var json = JsonSerializer.Serialize(values);
        if (json.Length > 4000) errors.Add("اطلاعات ثابت بیش از حد طولانی است.");
        if (errors.Count > 0) return new(false, null, errors);

        var quick = new QuickVoucher
        {
            Id = Guid.NewGuid(),
            VahedCode = c.VahedCode,
            TemplateId = template.Id,
            Title = title,
            ValuesJson = json,
            CreatedBy = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        await _repo.AddAsync(quick, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return new(true, quick.Id, []);
    }
}

// ─────────── حذف (نرم) — فقط سند پرتکرار همان واحد ───────────

public sealed record DeleteQuickVoucherCommand : IRequest<Unit>, IVahedScopedCommand
{
    [JsonIgnore]
    public Guid Id { get; set; }

    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class DeleteQuickVoucherHandler : IRequestHandler<DeleteQuickVoucherCommand, Unit>
{
    private readonly IQuickVoucherRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteQuickVoucherHandler(IQuickVoucherRepository repo, IUnitOfWork unitOfWork) { _repo = repo; _unitOfWork = unitOfWork; }

    public async Task<Unit> Handle(DeleteQuickVoucherCommand c, CancellationToken ct)
    {
        var quick = await _repo.GetForUpdateAsync(c.Id, ct);
        if (quick is null || quick.VahedCode != c.VahedCode) throw new NotFoundException("QuickVoucher", c.Id);
        quick.IsDeleted = true;
        await _unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
