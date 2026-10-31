using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Accrual;
using FluentValidation;
using MediatR;

namespace Accounting.Application.Accrual;

// ماژول RBAC «Accrual» (منوهای /accrual در MenuCatalog). واحد فقط از هدر؛ سطر واحد دیگر = «پیدا نشد».

// ─────────── فهرست زمان‌بندی‌ها ───────────

public sealed record ListAccrualSchedulesQuery(AccrualScheduleState? State, AccrualScheduleKind? Kind, string? From, string? To)
    : IRequest<IReadOnlyList<AccrualScheduleDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListAccrualSchedulesHandler : IRequestHandler<ListAccrualSchedulesQuery, IReadOnlyList<AccrualScheduleDto>>
{
    private readonly IAccrualScheduleRepository _repo;
    public ListAccrualSchedulesHandler(IAccrualScheduleRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<AccrualScheduleDto>> Handle(ListAccrualSchedulesQuery q, CancellationToken ct)
    {
        var rows = await _repo.ListAsync(q.VahedCode,
            new AccrualScheduleFilter(q.State, q.Kind, AccrualDates.Normalize(q.From), AccrualDates.Normalize(q.To)), ct);
        return await AccrualMapping.ToDtosAsync(_repo, rows, ct);
    }
}

// ─────────── سطرهای یک سند اصلی ───────────

public sealed record GetAccrualSchedulesByVoucherQuery(Guid VoucherId) : IRequest<AccrualVoucherScheduleDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetAccrualSchedulesByVoucherHandler : IRequestHandler<GetAccrualSchedulesByVoucherQuery, AccrualVoucherScheduleDto>
{
    private readonly IAccrualScheduleRepository _repo;
    public GetAccrualSchedulesByVoucherHandler(IAccrualScheduleRepository repo) => _repo = repo;

    public async Task<AccrualVoucherScheduleDto> Handle(GetAccrualSchedulesByVoucherQuery q, CancellationToken ct)
    {
        var rows = await _repo.ListBySourceAsync(q.VahedCode, q.VoucherId, tracking: false, ct);
        var dtos = await AccrualMapping.ToDtosAsync(_repo, rows, ct);
        return new AccrualVoucherScheduleDto(q.VoucherId, dtos.FirstOrDefault()?.SourceVoucherNo, rows.Sum(r => r.Amount), dtos);
    }
}

// ─────────── اجرا الان ───────────

public sealed record RunAccrualScheduleLineCommand : IRequest<AccrualRunResultDto>, IVahedScopedCommand
{
    /// <summary>از مسیر (<c>schedules/{id}/run</c>).</summary>
    [JsonIgnore]
    public Guid Id { get; set; }

    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class RunAccrualScheduleLineHandler : IRequestHandler<RunAccrualScheduleLineCommand, AccrualRunResultDto>
{
    private readonly AccrualScheduleRunner _runner;
    private readonly IAccrualScheduleRepository _repo;

    public RunAccrualScheduleLineHandler(AccrualScheduleRunner runner, IAccrualScheduleRepository repo)
    { _runner = runner; _repo = repo; }

    public async Task<AccrualRunResultDto> Handle(RunAccrualScheduleLineCommand c, CancellationToken ct)
    {
        var r = await _runner.RunAsync(c.Id, c.VahedCode, skipLocked: false, ct);
        var voucherNo = r.VoucherNo;
        if (voucherNo is null && r.VoucherId is Guid id)
            (await _repo.GetVoucherNumbersAsync([id], ct)).TryGetValue(id, out voucherNo);
        return new AccrualRunResultDto(r.VoucherId!.Value, voucherNo, r.Outcome == AccrualRunOutcome.AlreadyDone);
    }
}

// ─────────── لغو باقی‌مانده‌ها (فسخ قرارداد) ───────────

/// <summary>
/// این سطر و همهٔ سطرهای باز بعدیِ همان سند اصلی و همان نوع لغو می‌شوند (فسخ قرارداد پیش‌پرداخت). سطر اجراشده دست نمی‌خورد.
/// دلیل به انتهای شرح سطر اضافه می‌شود (ستون جدا ندارد) و لاگ تغییرات هم آن را دارد.
/// </summary>
public sealed record CancelAccrualScheduleCommand(string Reason) : IRequest<AccrualCancelResultDto>, IVahedScopedCommand
{
    [JsonIgnore]
    public Guid Id { get; set; }

    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class CancelAccrualScheduleValidator : AbstractValidator<CancelAccrualScheduleCommand>
{
    public CancelAccrualScheduleValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("دلیل لغو را بنویسید.")
            .MaximumLength(200).WithMessage("دلیل لغو حداکثر ۲۰۰ نویسه است.");
    }
}

public sealed class CancelAccrualScheduleHandler : IRequestHandler<CancelAccrualScheduleCommand, AccrualCancelResultDto>
{
    private readonly IAccrualScheduleRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CancelAccrualScheduleHandler(IAccrualScheduleRepository repo, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<AccrualCancelResultDto> Handle(CancelAccrualScheduleCommand c, CancellationToken ct)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // قفل همین سطر تا کار روزانه هم‌زمان اجرایش نکند؛ بقیهٔ سطرهای باز بعد از آن.
            var row = await _repo.LockAsync(c.Id, skipLocked: false, ct);
            if (row is null || row.VahedCode != c.VahedCode)
                throw new NotFoundException("AccrualSchedule", c.Id);
            if (row.State != AccrualScheduleState.Open)
                throw new BusinessRuleException(row.State == AccrualScheduleState.Done
                    ? "این سطر اجرا شده و سندش ساخته شده؛ لغو ممکن نیست. برای اصلاح، سند ساخته‌شده را برگردانید."
                    : "این سطر قبلاً لغو شده است.");

            var targets = (await _repo.ListBySourceAsync(c.VahedCode, row.SourceVoucherId, tracking: true, ct))
                .Where(r => r.Kind == row.Kind && r.LineNo >= row.LineNo && r.State == AccrualScheduleState.Open)
                .ToList();

            var now = DateTime.UtcNow;
            var note = $" — لغو: {c.Reason.Trim()}";
            foreach (var t in targets)
            {
                t.State = AccrualScheduleState.Cancelled;
                var desc = (t.Description ?? "") + note;
                t.Description = desc.Length <= 500 ? desc : desc[..500];
                t.ChangedBy = _currentUser.UserId;
                t.UpdatedAtUtc = now;
            }

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);
            return new AccrualCancelResultDto(targets.Count, targets.Sum(t => t.Amount));
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}

internal static class AccrualMapping
{
    public static async Task<IReadOnlyList<AccrualScheduleDto>> ToDtosAsync(
        IAccrualScheduleRepository repo, IReadOnlyList<AccrualSchedule> rows, CancellationToken ct)
    {
        var ids = rows.Select(r => r.SourceVoucherId)
            .Concat(rows.Where(r => r.ResultVoucherId.HasValue).Select(r => r.ResultVoucherId!.Value))
            .Distinct().ToList();
        IReadOnlyDictionary<Guid, string> numbers = ids.Count == 0 ? new Dictionary<Guid, string>() : await repo.GetVoucherNumbersAsync(ids, ct);

        return rows.Select(r => new AccrualScheduleDto(
            r.Id, r.SourceVoucherId, numbers.GetValueOrDefault(r.SourceVoucherId), r.ExecutionId,
            r.Kind, r.LineNo, r.DueDate, r.Amount, r.Description, r.State,
            r.ResultVoucherId, r.ResultVoucherId is Guid v ? numbers.GetValueOrDefault(v) : null,
            r.CreatedBy, r.CreatedAtUtc)).ToList();
    }
}
