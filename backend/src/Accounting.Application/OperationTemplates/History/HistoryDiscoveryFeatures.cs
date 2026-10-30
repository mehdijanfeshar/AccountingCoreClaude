using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using FluentValidation;
using MediatR;

namespace Accounting.Application.OperationTemplates.History;

// ═══════════════════ کشف الگوهای پرتکرار (G13) — ستاد و نقش مدیریتی کشور (فقط‌خواندنی) ═══════════════════

public sealed record ListHistoryPatternsQuery(
    int? FromYear, int? ToYear, string? VahedType, int MinCount, string? Search, int Page = 1, int PageSize = 20)
    : IRequest<HistoryPatternPageDto>;

public sealed class ListHistoryPatternsValidator : AbstractValidator<ListHistoryPatternsQuery>
{
    public ListHistoryPatternsValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("عبارت جستجو حداکثر ۱۰۰ نویسه است.");
        RuleFor(x => x.VahedType).MaximumLength(20).WithMessage("نوع واحد نامعتبر است.");
    }
}

public sealed class ListHistoryPatternsHandler : IRequestHandler<ListHistoryPatternsQuery, HistoryPatternPageDto>
{
    private readonly IHistoryPatternStore _store;
    private readonly ISubsidiaryAccountReader _accounts;
    private readonly IOperationTemplateRepository _templates;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;
    private readonly IHistoryMeaningStore? _meanings;

    public ListHistoryPatternsHandler(IHistoryPatternStore store, ISubsidiaryAccountReader accounts,
        IOperationTemplateRepository templates, ICurrentUser currentUser, IHeadquartersAccessService? hq = null,
        IHistoryMeaningStore? meanings = null)
    { _store = store; _accounts = accounts; _templates = templates; _currentUser = currentUser; _hq = hq; _meanings = meanings; }

    public async Task<HistoryPatternPageDto> Handle(ListHistoryPatternsQuery q, CancellationToken ct)
    {
        var country = _hq is null ? _currentUser.IsInRole(AppRoles.SetadAdmin) : await _hq.CanSeeCountryAsync(ct);
        if (!country)
            throw new RoleAccessDeniedException("فهرست الگوهای سوابق فقط برای کاربر ستاد مرکزی یا نقش مدیریتی سطح کشور است.");

        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 5, 100);
        if (await _store.CountAsync(ct) == 0)
            return new HistoryPatternPageDto([], 0, page, size, NeedsRebuild: true);

        var result = await _store.ListAsync(new HistoryPatternFilter(q.FromYear, q.ToYear, q.VahedType?.Trim(),
            Math.Max(0, q.MinCount), q.Search?.Trim(), (page - 1) * size, size), ct);

        var parsed = result.Items.Select(p => (Pattern: p, Lines: HistoryJson.Lines(p.LinesJson))).ToList();
        var moinIds = parsed.SelectMany(x => x.Lines).Select(l => l.MoinId).Distinct().ToList();
        var accounts = moinIds.Count == 0 ? new Dictionary<Guid, SubsidiaryAccountInfo>()
            : new Dictionary<Guid, SubsidiaryAccountInfo>(await _accounts.GetByIdsAsync(moinIds, ct));
        var groupIds = parsed.SelectMany(x => x.Lines).SelectMany(l => l.Levels).SelectMany(l => l.GroupIds).Distinct().ToList();
        var titles = groupIds.Count == 0 ? new Dictionary<Guid, string>()
            : new Dictionary<Guid, string>(await _store.GroupTitlesAsync(groupIds, ct));
        var meanings = _meanings is null || parsed.Count == 0 ? new Dictionary<Guid, IReadOnlyList<HistoryPatternMeaning>>()
            : new Dictionary<Guid, IReadOnlyList<HistoryPatternMeaning>>(await _meanings.GetForAsync(parsed.Select(x => x.Pattern.Id).ToList(), ct));

        // پوشش: الگوی فعالی که مجموعهٔ (معین، سمت) ردیف‌هایش با امضا برابر است
        var covered = new Dictionary<string, CoveredByTemplateDto>(StringComparer.Ordinal);
        foreach (var t in await _templates.ListActiveWithParametersAsync(ct))
        {
            var sig = HistoryJson.SignatureOf(t.Lines.Select(l => (l.SubsidiaryAccountId, (int)l.Side)));
            covered.TryAdd(sig, new CoveredByTemplateDto(t.Id, t.Title));
        }

        var items = parsed.Select(x => new HistoryPatternDto(
            x.Pattern.Id,
            x.Lines.Select(l =>
            {
                accounts.TryGetValue(l.MoinId, out var acc);
                return new HistoryLineDto(l.MoinId, acc?.Code, acc?.Title, l.Side, l.Share,
                    l.Levels.Select(lv => new HistoryDetailLevelDto(lv.Level,
                        lv.GroupIds.Select(g => titles.GetValueOrDefault(g) ?? g.ToString("D")).ToList())).ToList());
            }).ToList(),
            x.Pattern.VoucherCount ?? 0, x.Pattern.UnitCount ?? 0, x.Pattern.Years, x.Pattern.MedianAmount ?? 0, x.Pattern.LastDate,
            HistoryJson.Keywords(x.Pattern.KeywordsJson).Select(k => new KeywordDto(k.Word, k.Count)).ToList(),
            HistoryJson.Strings(x.Pattern.SampleDescsJson).Select(s => HistoryText.MaskForOtherUnits(s)).Where(s => s.Length > 0).Distinct(StringComparer.Ordinal).ToList(),
            covered.GetValueOrDefault(x.Pattern.Signature),
            MeaningDtos(meanings.GetValueOrDefault(x.Pattern.Id)))).ToList();

        return new HistoryPatternPageDto(items, result.Total, page, size, NeedsRebuild: false);
    }

    /// <summary>فقط الگوی چندمعنایی فهرست معنا دارد؛ یک‌معنایی ⇒ خالی.</summary>
    private static IReadOnlyList<HistoryMeaningDto> MeaningDtos(IReadOnlyList<HistoryPatternMeaning>? rows) =>
        rows is not { Count: > 1 } ? [] : rows.Select(m => new HistoryMeaningDto(m.Index, m.Title, m.Share ?? 0,
            HistoryJson.Strings(m.SamplesJson), HistoryJson.Keywords(m.KeywordsJson).Select(k => new KeywordDto(k.Word, k.Count)).ToList())).ToList();
}

// ═══════════════════ تبدیل به الگو — بدون ذخیره، برای پیش‌پر کردن طراح الگو ═══════════════════

/// <summary>همان شکل بدنهٔ ساخت الگو (<c>CreateOperationTemplateCommand</c>) + خطاها و نکته‌ها؛ هیچ‌چیز ذخیره نمی‌شود.</summary>
public sealed record HistoryTemplateDraftDto(
    string Code, string Title, string Description, string VoucherDescriptionPattern,
    List<TemplateParameterDto> Parameters, List<TemplateLineDto> Lines,
    Guid? SystemTypeId, string? Keywords, IReadOnlyList<string>? AllowedVahedTypes,
    string? PlainPattern, long? MaxAmount, bool RequireAttachment, RiskLevel? RiskBase, TemplateCategory? Category,
    IReadOnlyList<string> ValidationErrors, IReadOnlyList<string> Notes);

/// <param name="Meaning">اندیس معنا (الگوی چندمعنایی): عنوان، کلمه‌های کلیدی و نمونه‌ها از همان معنا؛ حساب‌ها یکی‌اند.</param>
public sealed record HistoryPatternToTemplateQuery(Guid PatternId, int? Meaning = null) : IRequest<HistoryTemplateDraftDto>;

public sealed class HistoryPatternToTemplateHandler : IRequestHandler<HistoryPatternToTemplateQuery, HistoryTemplateDraftDto>
{
    private readonly IHistoryPatternStore _store;
    private readonly HistoryTemplateFactory _factory;
    private readonly TemplateDefinitionValidator _validator;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;
    private readonly IHistoryMeaningStore? _meanings;

    public HistoryPatternToTemplateHandler(IHistoryPatternStore store, HistoryTemplateFactory factory,
        TemplateDefinitionValidator validator, ICurrentUser currentUser, IHeadquartersAccessService? hq = null,
        IHistoryMeaningStore? meanings = null)
    { _store = store; _factory = factory; _validator = validator; _currentUser = currentUser; _hq = hq; _meanings = meanings; }

    public async Task<HistoryTemplateDraftDto> Handle(HistoryPatternToTemplateQuery q, CancellationToken ct)
    {
        await TemplateMapping.EnsureAbilityAsync(_currentUser, _hq, AbilityCatalog.TemplatesDefine, "الگوی عملیات", ct);
        if (await _store.CountAsync(ct) == 0) throw new HistoryPatternsUnavailableException();
        var pattern = await _store.GetAsync(q.PatternId, ct) ?? throw new NotFoundException("HistoryPattern", q.PatternId);

        HistoryPatternMeaning? meaning = null;
        if (q.Meaning is { } index)
        {
            var rows = _meanings is null ? null : (await _meanings.GetForAsync([pattern.Id], ct)).GetValueOrDefault(pattern.Id);
            meaning = rows?.FirstOrDefault(m => m.Index == index) ?? throw new NotFoundException("HistoryPatternMeaning", pattern.Id);
            pattern = ForMeaning(pattern, meaning);
        }

        var built = await _factory.BuildAsync(pattern, new HistoryBuildContext(null), ct);
        if (built.Template is not { } t)
            return new HistoryTemplateDraftDto("", "", "", "", [], [], null, null, null, null, null, false, null, null, built.Problems, built.Notes);

        if (meaning is not null)
        {
            // هر معنا الگوی جدا: کد یکتا و عنوان از شرح نماینده
            var title = HistoryTemplateFactory.Sanitize(meaning.Title, 180);
            if (title.Length > 0)
            {
                t.VoucherDescriptionPattern = t.VoucherDescriptionPattern.Replace(t.Title, title, StringComparison.Ordinal);
                t.PlainPattern = t.PlainPattern?.Replace(t.Title, title, StringComparison.Ordinal);
                t.Title = title;
            }
            t.Code = $"{t.Code}_{meaning.Index + 1}";
            t.Description += $" معنای «{title}» (حدود {meaning.Share ?? 0}٪ شرح‌ها).";
        }

        var errors = await _validator.ValidateAsync(t, ct);
        return new HistoryTemplateDraftDto(
            t.Code, t.Title, t.Description, t.VoucherDescriptionPattern,
            HistoryTemplateFactory.Questions(t), HistoryTemplateFactory.LineDtos(t),
            t.SystemTypeId, t.Keywords, TemplateMapping.SplitVahedTypes(t.AllowedVahedTypes),
            t.PlainPattern, t.MaxAmount, t.RequireAttachment, t.RiskBase, t.Category, errors, built.Notes);
    }

    /// <summary>نسخهٔ الگو با کلمه‌ها و نمونه‌های یک معنا؛ تعداد سند به نسبت سهم معنا.</summary>
    public static HistoryPattern ForMeaning(HistoryPattern p, HistoryPatternMeaning m) => new()
    {
        Id = p.Id, Signature = p.Signature, LinesJson = p.LinesJson,
        VoucherCount = (int)Math.Round((p.VoucherCount ?? 0) * (m.Share ?? 100) / 100m, MidpointRounding.AwayFromZero),
        UnitCount = p.UnitCount, Years = p.Years, FromYear = p.FromYear, ToYear = p.ToYear, MedianAmount = p.MedianAmount,
        LastDate = p.LastDate, KeywordsJson = m.KeywordsJson, SampleDescsJson = m.SamplesJson, VahedTypes = p.VahedTypes,
        RebuiltAtUtc = p.RebuiltAtUtc,
    };
}

// ═══════════════════ حذف پیشنهاد کشف‌شده (DDL 086) ═══════════════════

public sealed record DeleteHistoryPatternCommand(Guid Id) : IRequest<HistoryDeleteResult>;

public sealed class DeleteHistoryPatternHandler : IRequestHandler<DeleteHistoryPatternCommand, HistoryDeleteResult>
{
    private readonly IHistoryPatternStore _store;
    private readonly IOperationTemplateRepository _templates;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;

    public DeleteHistoryPatternHandler(IHistoryPatternStore store, IOperationTemplateRepository templates, IUnitOfWork unitOfWork,
        ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    { _store = store; _templates = templates; _unitOfWork = unitOfWork; _currentUser = currentUser; _hq = hq; }

    public async Task<HistoryDeleteResult> Handle(DeleteHistoryPatternCommand c, CancellationToken ct)
    {
        await TemplateMapping.EnsureAbilityAsync(_currentUser, _hq, AbilityCatalog.TemplatesDefine, "سوابق حسابیار", ct);
        var pattern = await _store.GetAsync(c.Id, ct) ?? throw new NotFoundException("HistoryPattern", c.Id);

        // پوشش: الگوی فعالی که مجموعهٔ (معین، سمت) ردیف‌هایش با امضا برابر است (همان قاعدهٔ فهرست)
        foreach (var t in await _templates.ListActiveWithParametersAsync(ct))
            if (HistoryJson.SignatureOf(t.Lines.Select(l => (l.SubsidiaryAccountId, (int)l.Side))) == pattern.Signature)
                throw new HistoryPatternCoveredException();

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _store.DismissAsync(pattern, _currentUser.UserId, ct);
            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
        return new HistoryDeleteResult(1);
    }
}

// ═══════════════════ حذف گروهی پیشنهادهای پوشش‌داده‌نشده ═══════════════════

public sealed record DeleteUncoveredHistoryPatternsCommand(int? FromYear, int? ToYear, string? VahedType, int MinCount, string? Search)
    : IRequest<HistoryBulkDeleteResult>;

public sealed class DeleteUncoveredHistoryPatternsHandler : IRequestHandler<DeleteUncoveredHistoryPatternsCommand, HistoryBulkDeleteResult>
{
    private readonly IHistoryPatternStore _store;
    private readonly IOperationTemplateRepository _templates;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;

    public DeleteUncoveredHistoryPatternsHandler(IHistoryPatternStore store, IOperationTemplateRepository templates, IUnitOfWork unitOfWork,
        ICurrentUser currentUser, IHeadquartersAccessService? hq = null)
    { _store = store; _templates = templates; _unitOfWork = unitOfWork; _currentUser = currentUser; _hq = hq; }

    public async Task<HistoryBulkDeleteResult> Handle(DeleteUncoveredHistoryPatternsCommand c, CancellationToken ct)
    {
        await TemplateMapping.EnsureAbilityAsync(_currentUser, _hq, AbilityCatalog.TemplatesDefine, "سوابق حسابیار", ct);
        if (await _store.CountAsync(ct) == 0) throw new HistoryPatternsUnavailableException();

        var covered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in await _templates.ListActiveWithParametersAsync(ct))
            covered.Add(HistoryJson.SignatureOf(t.Lines.Select(l => (l.SubsidiaryAccountId, (int)l.Side))));

        var filter = new HistoryPatternFilter(c.FromYear, c.ToYear, c.VahedType?.Trim(), Math.Max(0, c.MinCount), c.Search?.Trim(), 0, int.MaxValue);
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var r = await _store.DismissUncoveredAsync(filter, covered, _currentUser.UserId, ct);
            await _unitOfWork.CommitTransactionAsync(ct);
            return new HistoryBulkDeleteResult(r.Count, r.SkippedCovered);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
