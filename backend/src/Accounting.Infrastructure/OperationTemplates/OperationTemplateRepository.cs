using Accounting.Application.OperationTemplates;
using Accounting.Domain.OperationTemplates;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.OperationTemplates;

/// <summary>
/// ذخیره‌سازی با همان <see cref="LegacyDbContext"/> و <c>IUnitOfWork</c> پروژه — ثبت سند و
/// <see cref="OperationExecution"/> در یک SaveChanges/تراکنش. (AnyAsync روی Oracle ممنوع: ORA-00904.)
/// </summary>
public sealed class OperationTemplateRepository : IOperationTemplateRepository
{
    private readonly LegacyDbContext _db;
    public OperationTemplateRepository(LegacyDbContext db) => _db = db;

    public Task<OperationTemplate?> GetFullAsync(Guid id, CancellationToken ct) =>
        _db.Set<OperationTemplate>()
           .AsNoTracking()
           .Include(t => t.Parameters)
           .Include(t => t.Lines).ThenInclude(l => l.Details)
           .AsSplitQuery()
           .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<OperationTemplate>> ListActiveWithParametersAsync(CancellationToken ct) =>
        await _db.Set<OperationTemplate>().AsNoTracking()
                 .Where(t => t.IsActive)
                 .Include(t => t.Parameters)
                 .Include(t => t.Lines).ThenInclude(l => l.Details)
                 .AsSplitQuery()
                 .OrderBy(t => t.Title).ToListAsync(ct);

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        await _db.Set<OperationTemplate>().CountAsync(t => t.Code == code, ct) > 0;

    public async Task<bool> CodeExistsForOtherAsync(string code, Guid? exceptId, CancellationToken ct) =>
        await _db.Set<OperationTemplate>().CountAsync(t => t.Code == code && (exceptId == null || t.Id != exceptId), ct) > 0;

    public async Task<IReadOnlyList<OperationTemplate>> ListAllAsync(CancellationToken ct) =>
        await _db.Set<OperationTemplate>().AsNoTracking()
                 .Include(t => t.Parameters)
                 .Include(t => t.Lines)
                 .AsSplitQuery()
                 .OrderBy(t => t.Title).ToListAsync(ct);

    public Task<OperationTemplate?> GetForUpdateAsync(Guid id, CancellationToken ct) =>
        _db.Set<OperationTemplate>()
           .Include(t => t.Parameters)
           .Include(t => t.Lines).ThenInclude(l => l.Details)
           .AsSplitQuery()
           .FirstOrDefaultAsync(t => t.Id == id, ct);

    public void RemoveChildren(OperationTemplate template)
    {
        _db.RemoveRange(template.Lines.SelectMany(l => l.Details));
        _db.RemoveRange(template.Lines);
        _db.RemoveRange(template.Parameters);
    }

    public void AddChildren(OperationTemplate template)
    {
        _db.AddRange(template.Parameters);
        _db.AddRange(template.Lines);
        _db.AddRange(template.Lines.SelectMany(l => l.Details));
    }

    public async Task AddAsync(OperationTemplate template, CancellationToken ct) =>
        await _db.Set<OperationTemplate>().AddAsync(template, ct);

    public Task<OperationExecution?> FindExecutionAsync(Guid id, CancellationToken ct) =>
        _db.Set<OperationExecution>().AsNoTracking().FirstOrDefaultAsync(x => x.ClientRequestId == id, ct);

    public async Task AddExecutionAsync(OperationExecution execution, CancellationToken ct) =>
        await _db.Set<OperationExecution>().AddAsync(execution, ct);

    public async Task<(IReadOnlyList<OperationExecution> Items, int Total)> ListExecutionsAsync(ExecutionFilter f, CancellationToken ct)
    {
        var q = _db.Set<OperationExecution>().AsNoTracking();
        if (f.VahedCode is not null) q = q.Where(x => x.VahedCode == f.VahedCode);
        if (f.CreatedBy is not null) q = q.Where(x => x.CreatedBy == f.CreatedBy);
        if (f.TemplateId is { } tid) q = q.Where(x => x.OperationTemplateId == tid);
        if (f.Channel is not null) q = q.Where(x => x.Channel == f.Channel);
        if (f.FromUtc is { } from) q = q.Where(x => x.CreatedAtUtc >= from);
        if (f.ToUtc is { } to) q = q.Where(x => x.CreatedAtUtc < to);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreatedAtUtc).Skip(f.Skip).Take(f.Take).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<TemplateUsageRow>> UsageAsync(string? vahedCode, CancellationToken ct)
    {
        var q = _db.Set<OperationExecution>().AsNoTracking().Where(x => x.OperationTemplateId != null);
        if (vahedCode is not null) q = q.Where(x => x.VahedCode == vahedCode);
        var rows = await q.GroupBy(x => x.OperationTemplateId)
            .Select(g => new { TemplateId = g.Key, Count = g.Count(), Last = g.Max(x => x.CreatedAtUtc) })
            .ToListAsync(ct);
        return rows.Select(r => new TemplateUsageRow(r.TemplateId!.Value, r.Count, r.Last)).ToList();
    }
}
