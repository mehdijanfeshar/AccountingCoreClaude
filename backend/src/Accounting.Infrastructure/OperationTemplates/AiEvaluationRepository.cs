using Accounting.Application.OperationTemplates.Agent;
using Accounting.Domain.Entity;
using Accounting.Domain.OperationTemplates;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>
/// سنجش هوش مصنوعی (DDL 088) و خواندن منبع اجرای سایه از <c>TB_OP_EXECUTION</c> ⋈ <c>TB_VOUCHERSHEAD</c>.
/// فقط stage؛ SaveChanges با هندلر. روی Oracle از <c>AnyAsync</c> و record سازنده‌دار در Select استفاده نمی‌شود.
/// </summary>
public sealed class AiEvaluationRepository : IAiEvaluationRepository
{
    private readonly LegacyDbContext _db;
    public AiEvaluationRepository(LegacyDbContext db) => _db = db;

    public async Task<IReadOnlyList<AiGoldenCase>> ListGoldenAsync(bool activeOnly, CancellationToken ct)
    {
        var q = _db.Set<AiGoldenCase>().AsNoTracking().Where(g => !g.IsDeleted);
        if (activeOnly) q = q.Where(g => g.IsActive);
        return await q.OrderByDescending(g => g.CreatedAtUtc).ToListAsync(ct);
    }

    public Task<AiGoldenCase?> FindGoldenAsync(Guid id, CancellationToken ct) =>
        _db.Set<AiGoldenCase>().FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, ct);

    public async Task AddGoldenAsync(AiGoldenCase golden, CancellationToken ct) =>
        await _db.Set<AiGoldenCase>().AddAsync(golden, ct);

    public async Task<IReadOnlySet<Guid>> GoldenSourceExecutionIdsAsync(CancellationToken ct) =>
        (await _db.Set<AiGoldenCase>().AsNoTracking()
            .Where(g => g.SourceExecutionId != null)
            .Select(g => g.SourceExecutionId!.Value)
            .ToListAsync(ct)).ToHashSet();

    public async Task AddRunAsync(AiEvalRun run, IReadOnlyList<AiEvalItem> items, CancellationToken ct)
    {
        await _db.Set<AiEvalRun>().AddAsync(run, ct);
        await _db.Set<AiEvalItem>().AddRangeAsync(items, ct);
    }

    public async Task<IReadOnlyList<AiEvalRun>> ListRunsAsync(int take, CancellationToken ct) =>
        await _db.Set<AiEvalRun>().AsNoTracking().OrderByDescending(r => r.CreatedAtUtc).Take(take).ToListAsync(ct);

    public Task<AiEvalRun?> FindRunAsync(Guid id, CancellationToken ct) =>
        _db.Set<AiEvalRun>().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<AiEvalItem>> ListItemsAsync(Guid runId, CancellationToken ct) =>
        await _db.Set<AiEvalItem>().AsNoTracking().Where(i => i.RunId == runId).OrderBy(i => i.MaskedSentence).ToListAsync(ct);

    public async Task<IReadOnlyList<ShadowSourceRow>> ListShadowSourcesAsync(string? vahedCode, int take, CancellationToken ct)
    {
        var q =
            from e in _db.Set<OperationExecution>().AsNoTracking()
            join h in _db.Set<TB_VOUCHERSHEAD>().AsNoTracking() on e.VoucherId equals h.ID
            where h.ISDELETED != true && e.UserSentence != null && e.AiTraceId == null && e.Channel == "Form"
            select e;
        if (vahedCode is not null) q = q.Where(e => e.VahedCode == vahedCode);
        var rows = await q.OrderByDescending(e => e.CreatedAtUtc).Take(take).ToListAsync(ct);
        return rows.Select(e => new ShadowSourceRow(e.Id, e.VahedCode, e.UserSentence!, e.TemplateCode, e.InputJson)).ToList();
    }
}
