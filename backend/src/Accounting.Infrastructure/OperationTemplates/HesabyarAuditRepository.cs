using Accounting.Application.OperationTemplates;
using Accounting.Domain.Entity;
using Accounting.Domain.OperationTemplates;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>
/// ردپای هوش مصنوعی (DDL 083)، تاریخچهٔ بررسی (DDL 081) و خواندن صف بررسی/«سندهای من» روی
/// <c>TB_OP_EXECUTION</c> ⋈ <c>TB_VOUCHERSHEAD</c>. فقط stage؛ SaveChanges با هندلر.
/// روی Oracle از <c>AnyAsync</c> استفاده نمی‌شود (ORA-00904).
/// </summary>
public sealed class HesabyarAuditRepository : IHesabyarAuditRepository
{
    private readonly LegacyDbContext _db;
    public HesabyarAuditRepository(LegacyDbContext db) => _db = db;

    // ─────────── ردپای هوش مصنوعی ───────────

    public async Task AddTraceAsync(OperationAiTrace trace, CancellationToken ct) =>
        await _db.Set<OperationAiTrace>().AddAsync(trace, ct);

    public Task<OperationAiTrace?> FindTraceAsync(Guid id, CancellationToken ct) =>
        _db.Set<OperationAiTrace>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<AiTraceRow>> ListTracesAsync(string vahedCode, Guid? executionId, int take, CancellationToken ct)
    {
        if (executionId is { } eid)
        {
            var traceId = await _db.Set<OperationExecution>().AsNoTracking()
                .Where(e => e.Id == eid && e.VahedCode == vahedCode)
                .Select(e => e.AiTraceId)
                .FirstOrDefaultAsync(ct);
            if (traceId is null) return [];
            var trace = await FindTraceAsync(traceId.Value, ct);
            return trace is null ? [] : [new AiTraceRow(trace, eid)];
        }

        var traces = await _db.Set<OperationAiTrace>().AsNoTracking()
            .Where(t => t.VahedCode == vahedCode)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);
        var ids = traces.Select(t => (Guid?)t.Id).ToList();
        var links = await _db.Set<OperationExecution>().AsNoTracking()
            .Where(e => ids.Contains(e.AiTraceId))
            .Select(e => new { e.Id, e.AiTraceId })
            .ToListAsync(ct);
        var byTrace = links.Where(l => l.AiTraceId != null).GroupBy(l => l.AiTraceId!.Value).ToDictionary(g => g.Key, g => g.First().Id);
        return traces.Select(t => new AiTraceRow(t, byTrace.TryGetValue(t.Id, out var e) ? e : null)).ToList();
    }

    // ─────────── تاریخچهٔ بررسی ───────────

    public async Task AddReviewAsync(OperationReview review, CancellationToken ct) =>
        await _db.Set<OperationReview>().AddAsync(review, ct);

    public async Task<IReadOnlyList<OperationExecution>> ListExecutionsByVouchersAsync(IReadOnlyCollection<Guid> voucherIds, CancellationToken ct)
    {
        var ids = voucherIds.ToList();
        return await _db.Set<OperationExecution>().AsNoTracking().Where(e => ids.Contains(e.VoucherId)).ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, OperationReview>> LatestReviewsAsync(IReadOnlyCollection<Guid> executionIds, CancellationToken ct)
    {
        if (executionIds.Count == 0) return new Dictionary<Guid, OperationReview>();
        var ids = executionIds.ToList();
        // «پاسخ» (Replied) وضعیت نیست؛ جدا از آخرین بررسی خوانده می‌شود (LatestRepliesAsync).
        var reviews = await _db.Set<OperationReview>().AsNoTracking()
            .Where(r => ids.Contains(r.ExecutionId) && r.Action != ReviewAction.Replied).ToListAsync(ct);
        return reviews.GroupBy(r => r.ExecutionId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAtUtc).First());
    }

    // ─────────── خواندن صف و سندهای من ───────────

    // کلاس با init (نه record سازنده‌دار): EF فقط Where روی member-init را به SQL ترجمه می‌کند.
    private sealed class Joined
    {
        public Guid ExecutionId { get; init; }
        public Guid VoucherId { get; init; }
        public string? DocNum { get; init; }
        public string? DateDoc { get; init; }
        public DocLife? Life { get; init; }
        public string? Description { get; init; }
        public Guid? TemplateId { get; init; }
        public string TemplateCode { get; init; } = "";
        public string Channel { get; init; } = "";
        public string VahedCode { get; init; } = "";
        public string CreatedBy { get; init; } = "";
        public DateTime CreatedAtUtc { get; init; }
        public string? Year { get; init; }
        public RiskLevel? Risk { get; init; }
        public string? WarningsJson { get; init; }
        public string? RiskReasonsJson { get; init; }
        public string? UserSentence { get; init; }
        public string? AnswerSourcesJson { get; init; }
        public bool? AiConfirmed { get; init; }
        public string? AttachFileName { get; init; }
        public bool HasAttachment => !string.IsNullOrEmpty(AttachFileName);
    }

    private IQueryable<Joined> JoinedQuery() =>
        from e in _db.Set<OperationExecution>().AsNoTracking()
        join h in _db.Set<TB_VOUCHERSHEAD>().AsNoTracking() on e.VoucherId equals h.ID
        where h.ISDELETED != true
        select new Joined
        {
            ExecutionId = e.Id, VoucherId = h.ID, DocNum = h.DOC_NUM, DateDoc = h.DATE_DOC, Life = h.DOCLIFE,
            Description = h.HEAD_DESC, TemplateId = e.OperationTemplateId, TemplateCode = e.TemplateCode,
            Channel = e.Channel, VahedCode = e.VahedCode, CreatedBy = e.CreatedBy, CreatedAtUtc = e.CreatedAtUtc,
            Year = h.YEAR, Risk = e.RiskLevel, WarningsJson = e.WarningsJson, RiskReasonsJson = e.RiskReasonsJson,
            UserSentence = e.UserSentence, AnswerSourcesJson = e.AnswerSourcesJson, AiConfirmed = e.AiConfirmed,
            AttachFileName = h.ATTACHFILE_NAME
        };

    private IQueryable<Joined> ByLife(string vahedCode, DocLife life, string? year)
    {
        var q = JoinedQuery().Where(x => x.VahedCode == vahedCode && x.Life == life);
        if (!string.IsNullOrWhiteSpace(year))
        {
            var y = year.Trim();
            q = q.Where(x => x.Year == y);
        }
        return q;
    }

    public async Task<(int All, int Low, int Attention)> CountByRiskAsync(string vahedCode, DocLife life, string? year, CancellationToken ct)
    {
        // خالی (اجرای پیش از DDL 084) = پایین
        var q = ByLife(vahedCode, life, year);
        var all = await q.CountAsync(ct);
        var attention = await q.CountAsync(x => x.Risk == null || x.Risk == RiskLevel.Medium || x.Risk == RiskLevel.High, ct);
        return (all, all - attention, attention);
    }

    public async Task<(IReadOnlyList<ReviewVoucherRow> Items, int Total)> ListByLifeAsync(
        string vahedCode, DocLife life, string? year, int skip, int take, CancellationToken ct, string? risk = null)
    {
        var q = ByLife(vahedCode, life, year);
        q = risk switch
        {
            "low" => q.Where(x => x.Risk == RiskLevel.Low),
            "attention" => q.Where(x => x.Risk == null || x.Risk == RiskLevel.Medium || x.Risk == RiskLevel.High),
            _ => q
        };
        var total = await q.CountAsync(ct);
        var page = await q.OrderBy(x => x.CreatedAtUtc).Skip(skip).Take(take).ToListAsync(ct);
        return (await EnrichAsync(page, ct), total);
    }

    public async Task<IReadOnlyList<ReviewVoucherRow>> ListMineAsync(string userId, string vahedCode, string? year, int take, CancellationToken ct)
    {
        var q = JoinedQuery().Where(x => x.VahedCode == vahedCode && x.CreatedBy == userId);
        if (!string.IsNullOrWhiteSpace(year))
        {
            var y = year.Trim();
            q = q.Where(x => x.Year == y);
        }
        var page = await q.OrderByDescending(x => x.CreatedAtUtc).Take(take).ToListAsync(ct);
        return await EnrichAsync(page, ct);
    }

    public async Task<IReadOnlyList<ReviewVoucherRow>> GetRowsAsync(IReadOnlyCollection<Guid> executionIds, CancellationToken ct)
    {
        var ids = executionIds.ToList();
        var rows = await JoinedQuery().Where(x => ids.Contains(x.ExecutionId)).ToListAsync(ct);
        return await EnrichAsync(rows, ct);
    }

    /// <summary>عنوان الگو و جمع بدهکار سند (مبلغ کل) برای همین صفحه از ردیف‌ها.</summary>
    private async Task<IReadOnlyList<ReviewVoucherRow>> EnrichAsync(List<Joined> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return [];

        var templateIds = rows.Where(r => r.TemplateId != null).Select(r => (Guid?)r.TemplateId!.Value).Distinct().ToList();
        var templates = templateIds.Count == 0
            ? new Dictionary<Guid, (string Title, bool RequireAttachment)>()
            : (await _db.Set<OperationTemplate>().AsNoTracking()
                    .Where(t => templateIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.Title, t.RequireAttachment })
                    .ToListAsync(ct))
                .ToDictionary(t => t.Id, t => (t.Title, t.RequireAttachment));

        var voucherIds = rows.Select(r => (Guid?)r.VoucherId).Distinct().ToList();
        var sums = (await _db.Set<TB_VOUCHERSDETAIL>().AsNoTracking()
                .Where(d => voucherIds.Contains(d.VOUCHERSHEAD_ID) && d.ISDELETED != true)
                .GroupBy(d => d.VOUCHERSHEAD_ID)
                .Select(g => new { Key = g.Key, Total = g.Sum(x => x.DEBTOR) })
                .ToListAsync(ct))
            .Where(s => s.Key != null)
            .ToDictionary(s => s.Key!.Value, s => s.Total ?? 0m);

        // پیوست واقعی روی TB_ATTACH (همان جدول برنامهٔ قدیم)؛ ATTACHFILE_NAME سرسند فقط برای سندهای قدیمی.
        var attachCounts = (await _db.Set<TB_ATTACH>().AsNoTracking()
                .Where(a => voucherIds.Contains(a.VOUCHERSHEAD_ID) && a.ISDELETED != true)
                .GroupBy(a => a.VOUCHERSHEAD_ID)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .Where(a => a.Key != null)
            .ToDictionary(a => a.Key!.Value, a => a.Count);

        return rows.Select(r => new ReviewVoucherRow(
            r.ExecutionId, r.VoucherId, r.DocNum, r.DateDoc, r.Life, r.Description, r.TemplateId, r.TemplateCode,
            r.TemplateId is { } tid && templates.TryGetValue(tid, out var tpl) ? tpl.Title
                // فاز ۶۳: ساخته‌شده از سوابق ⇒ بدون الگو؛ عنوان «از سوابق: …»
                : r.TemplateId is null && Accounting.Application.OperationTemplates.History.HistoryDefaults.IsHistoryCode(r.TemplateCode)
                    ? Accounting.Application.OperationTemplates.History.HistoryDefaults.Title(r.UserSentence ?? r.Description)
                    : null,
            r.Channel, r.VahedCode, r.CreatedBy, r.CreatedAtUtc,
            (long)Math.Round(sums.TryGetValue(r.VoucherId, out var s) ? s : 0m, MidpointRounding.AwayFromZero),
            r.Risk, r.WarningsJson, r.RiskReasonsJson, r.UserSentence, r.AnswerSourcesJson, r.AiConfirmed,
            r.TemplateId is { } rid && templates.TryGetValue(rid, out var rt) && rt.RequireAttachment,
            r.HasAttachment || attachCounts.ContainsKey(r.VoucherId), attachCounts.GetValueOrDefault(r.VoucherId))).ToList();
    }

    // ─────────── فاز ۶۲: پاسخ‌ها، تاریخچه، درخواست‌ها (DDL 084) ───────────

    public async Task<IReadOnlyDictionary<Guid, OperationReview>> LatestRepliesAsync(IReadOnlyCollection<Guid> executionIds, CancellationToken ct)
    {
        if (executionIds.Count == 0) return new Dictionary<Guid, OperationReview>();
        var ids = executionIds.ToList();
        var replies = await _db.Set<OperationReview>().AsNoTracking()
            .Where(r => ids.Contains(r.ExecutionId) && r.Action == ReviewAction.Replied).ToListAsync(ct);
        return replies.GroupBy(r => r.ExecutionId).ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAtUtc).First());
    }

    public async Task<IReadOnlyList<OperationReview>> ListReviewsAsync(Guid executionId, CancellationToken ct) =>
        await _db.Set<OperationReview>().AsNoTracking().Where(r => r.ExecutionId == executionId)
            .OrderBy(r => r.CreatedAtUtc).ToListAsync(ct);

    public async Task AddRequestAsync(OperationRequest request, CancellationToken ct) =>
        await _db.Set<OperationRequest>().AddAsync(request, ct);

    public async Task<IReadOnlyList<OperationRequest>> ListRequestsAsync(string vahedCode, RequestStatus? status, int take, CancellationToken ct)
    {
        var q = _db.Set<OperationRequest>().AsNoTracking().Where(r => r.VahedCode == vahedCode);
        if (status is { } s) q = q.Where(r => r.Status == s);
        return await q.OrderByDescending(r => r.CreatedAtUtc).Take(take).ToListAsync(ct);
    }

    public Task<OperationRequest?> FindRequestForUpdateAsync(Guid id, CancellationToken ct) =>
        _db.Set<OperationRequest>().FirstOrDefaultAsync(r => r.Id == id, ct);
}
