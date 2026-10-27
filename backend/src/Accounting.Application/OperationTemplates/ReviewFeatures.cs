using System.Text.Json.Serialization;
using Accounting.Application.Commands.Vouchers.ChangeVoucherState;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.Entity;
using Accounting.Domain.OperationTemplates;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.OperationTemplates;

// ═══════════════════════ فاز ۶۱ — صف بررسی حسابیار، «سندهای من» (DDL 081) ═══════════════════════
//
// چرخهٔ سند: Execute ⇒ موقت ⇒ (بررسی شد) ⇒ بررسی‌شده، یا (برگشت با دلیل) ⇒ یادداشت ⇒ اصلاح و موقت دوباره.
// وضعیت فعلی همیشه از DOCLIFE سند است؛ TB_OP_REVIEW فقط تاریخچه و پیام برگشت.
// «بررسی شد» = همان ChangeVoucherStateCommand؛ تفکیک وظایف (G1) و ثبت ردیف تاریخچه در
// IHesabyarVoucherStateHook (داخل همان تراکنش) انجام می‌شود، پس هر مسیری به «بررسی‌شده» پوشش دارد.

/// <summary>
/// ردپای حسابیار: ردپای هوش مصنوعی (DDL 083)، تاریخچهٔ بررسی (DDL 081) و خواندن‌های صف/سندهای من.
/// فقط stage می‌کند؛ SaveChanges با هندلر.
/// </summary>
public interface IHesabyarAuditRepository
{
    Task AddTraceAsync(OperationAiTrace trace, CancellationToken ct);
    Task<OperationAiTrace?> FindTraceAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<AiTraceRow>> ListTracesAsync(string vahedCode, Guid? executionId, int take, CancellationToken ct);

    Task AddReviewAsync(OperationReview review, CancellationToken ct);
    Task<IReadOnlyList<OperationExecution>> ListExecutionsByVouchersAsync(IReadOnlyCollection<Guid> voucherIds, CancellationToken ct);

    /// <summary>ریسک/دلیل/هشدار یک اجرای خوانده‌شده (untracked) را برای ذخیره stage می‌کند.</summary>
    Task UpdateExecutionRiskAsync(OperationExecution execution, CancellationToken ct);

    /// <summary>آخرین ردیف بررسی هر اجرا (کلید = ExecutionId).</summary>
    Task<IReadOnlyDictionary<Guid, OperationReview>> LatestReviewsAsync(IReadOnlyCollection<Guid> executionIds, CancellationToken ct);

    /// <summary>سندهای حسابیار واحد در وضعیت <paramref name="life"/> (غیرحذف‌شده)، قدیمی‌ترین اول.</summary>
    /// <param name="risk">null = همه؛ <c>low</c> = پایین (و بدون ریسک ثبت‌شده)؛ <c>attention</c> = متوسط و بالا.</param>
    Task<(IReadOnlyList<ReviewVoucherRow> Items, int Total)> ListByLifeAsync(
        string vahedCode, DocLife life, string? year, int skip, int take, CancellationToken ct, string? risk = null);

    /// <summary>شمارنده‌های صف بر پایهٔ ریسک (کل، پایین، نیازمند توجه) برای همان واحد/وضعیت.</summary>
    Task<(int All, int Low, int Attention)> CountByRiskAsync(string vahedCode, DocLife life, string? year, CancellationToken ct);

    /// <summary>آخرین «پاسخ ثبت‌کننده» (Replied) هر اجرا.</summary>
    Task<IReadOnlyDictionary<Guid, OperationReview>> LatestRepliesAsync(IReadOnlyCollection<Guid> executionIds, CancellationToken ct);

    /// <summary>کل تاریخچهٔ بررسی یک اجرا، قدیمی‌ترین اول.</summary>
    Task<IReadOnlyList<OperationReview>> ListReviewsAsync(Guid executionId, CancellationToken ct);

    Task AddRequestAsync(OperationRequest request, CancellationToken ct);
    Task<IReadOnlyList<OperationRequest>> ListRequestsAsync(string vahedCode, RequestStatus? status, int take, CancellationToken ct);
    Task<OperationRequest?> FindRequestForUpdateAsync(Guid id, CancellationToken ct);

    /// <summary>سندهای حسابیار یک کاربر در یک واحد (غیرحذف‌شده)، جدیدترین اول.</summary>
    Task<IReadOnlyList<ReviewVoucherRow>> ListMineAsync(string userId, string vahedCode, string? year, int take, CancellationToken ct);

    /// <summary>ردیف‌های اجرا (با وضعیت فعلی سند) برای شناسه‌های اجرا؛ سند حذف‌شده برنمی‌گردد.</summary>
    Task<IReadOnlyList<ReviewVoucherRow>> GetRowsAsync(IReadOnlyCollection<Guid> executionIds, CancellationToken ct);
}

public sealed record AiTraceRow(OperationAiTrace Trace, Guid? ExecutionId);

public sealed record ReviewVoucherRow(
    Guid ExecutionId, Guid VoucherId, string? DocNum, string? DateDoc, DocLife? Life, string? Description,
    Guid? TemplateId, string TemplateCode, string? TemplateTitle, string Channel, string VahedCode,
    string CreatedBy, DateTime CreatedAtUtc, long Amount,
    RiskLevel? Risk = null, string? WarningsJson = null, string? RiskReasonsJson = null, string? UserSentence = null,
    string? AnswerSourcesJson = null, bool? AiConfirmed = null, bool RequireAttachment = false, bool HasAttachment = false,
    int AttachmentCount = 0);

internal static class ReviewAccess
{
    /// <summary>بررسی‌کنندگان: مسئول حسابداری و مدیرهای واحد (مدیر ستاد، مدیر درمانی، مدیر بیمه‌ای).</summary>
    public static readonly string[] Reviewers = [AppRoles.SetadAdmin, AppRoles.MaliAdmin, AppRoles.HltAdmin, AppRoles.EdkAdmin];

    public static void EnsureReviewer(ICurrentUser user)
    {
        if (!Reviewers.Any(user.IsInRole))
            throw new RoleAccessDeniedException("بررسی سندهای حسابیار فقط برای مسئول حسابداری و مدیران واحد مجاز است.");
    }

    public static string Status(DocLife? life, OperationReview? latest) => life switch
    {
        DocLife.Temporary => "pending",
        DocLife.Reviewed or DocLife.Accepted => "reviewed",
        DocLife.Draft when latest?.Action == ReviewAction.Returned => "returned",
        _ => "draft",
    };
}

// ─────────── قلاب تغییر وضعیت سند (G1 + تاریخچه) ───────────

/// <summary>
/// از <see cref="ChangeVoucherStateCommandHandler"/> قبل از SaveChanges صدا زده می‌شود (یک تراکنش).
/// فقط روی سندهایی اثر دارد که ردیف <c>TB_OP_EXECUTION</c> دارند.
/// </summary>
public interface IHesabyarVoucherStateHook
{
    Task BeforeStateChangeAsync(IReadOnlyCollection<TB_VOUCHERSHEAD> heads, DocLife newState, CancellationToken ct);
}

public sealed class HesabyarVoucherStateHook : IHesabyarVoucherStateHook
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly ICurrentUser _currentUser;

    public HesabyarVoucherStateHook(IHesabyarAuditRepository repo, ICurrentUser currentUser)
    { _repo = repo; _currentUser = currentUser; }

    public async Task BeforeStateChangeAsync(IReadOnlyCollection<TB_VOUCHERSHEAD> heads, DocLife newState, CancellationToken ct)
    {
        // فقط انتقال به «بررسی‌شده» (G1) یا «موقت» (ارسال دوباره پس از برگشت) به حسابیار مربوط است.
        if (newState is not (DocLife.Reviewed or DocLife.Temporary)) return;

        var changing = heads.Where(h => h.DOCLIFE != newState).Select(h => h.ID).ToList();
        if (changing.Count == 0) return;

        var executions = await _repo.ListExecutionsByVouchersAsync(changing, ct);
        if (executions.Count == 0) return;

        var me = _currentUser.UserId;
        var now = DateTime.UtcNow;

        if (newState == DocLife.Reviewed)
        {
            // G1: تفکیک وظایف — فقط برای سندهای دارای ردیف اجرا. کل دسته رد می‌شود (همه یا هیچ).
            if (executions.Any(e => string.Equals(e.CreatedBy, me, StringComparison.Ordinal)))
                throw new HesabyarSelfReviewDeniedException();

            // ریسک بالا (سقف مبلغ یا ریسک پایهٔ الگو): فقط مسئول حسابداری (مدیر مالی) تأیید می‌کند.
            if (executions.Any(e => e.RiskLevel == RiskLevel.High) && !_currentUser.IsInRole(AppRoles.MaliAdmin))
                throw new RoleAccessDeniedException("تأیید سند با ریسک بالا فقط با نقش مسئول حسابداری (مدیر مالی) ممکن است.");

            foreach (var e in executions)
                await _repo.AddReviewAsync(NewReview(e, ReviewAction.Approved, null, null, me, now), ct);
            return;
        }

        // موقت: اگر آخرین ردیف بررسی «برگشت» بود ⇒ «ارسال دوباره».
        var latest = await _repo.LatestReviewsAsync(executions.Select(e => e.Id).ToList(), ct);
        foreach (var e in executions.Where(e => latest.TryGetValue(e.Id, out var r) && r.Action == ReviewAction.Returned))
            await _repo.AddReviewAsync(NewReview(e, ReviewAction.Resubmitted, null, null, me, now), ct);
    }

    internal static OperationReview NewReview(OperationExecution e, ReviewAction action, string? reasonCode, string? message, string reviewer, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        ExecutionId = e.Id,
        VoucherId = e.VoucherId,
        Action = action,
        ReasonCode = reasonCode,
        Message = message,
        ReviewerId = reviewer,
        VahedCode = e.VahedCode,
        CreatedAtUtc = now,
    };
}

// ─────────── صف بررسی ───────────

// ─────────── بررسی شد (تکی و دسته‌ای، همه یا هیچ) ───────────

public sealed record ReviewActionResult(int Count);

public sealed record ApproveReviewsCommand(IReadOnlyList<Guid> ExecutionIds) : IRequest<ReviewActionResult>, IVahedScopedCommand
{
    public const int MaxBatchSize = 100;

    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ApproveReviewsValidator : AbstractValidator<ApproveReviewsCommand>
{
    public ApproveReviewsValidator()
    {
        RuleFor(x => x.ExecutionIds).NotEmpty().WithMessage("حداقل یک سند باید انتخاب شود.")
            .Must(ids => ids.Count <= ApproveReviewsCommand.MaxBatchSize).WithMessage($"حداکثر {ApproveReviewsCommand.MaxBatchSize} سند در هر بار.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("شناسهٔ سند تکراری است.");
    }
}

public sealed class ApproveReviewsHandler : IRequestHandler<ApproveReviewsCommand, ReviewActionResult>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;

    public ApproveReviewsHandler(IHesabyarAuditRepository repo, ICurrentUser currentUser, IMediator mediator)
    { _repo = repo; _currentUser = currentUser; _mediator = mediator; }

    public async Task<ReviewActionResult> Handle(ApproveReviewsCommand c, CancellationToken ct)
    {
        ReviewAccess.EnsureReviewer(_currentUser);
        var ids = c.ExecutionIds.Distinct().ToList();
        var rows = await _repo.GetRowsAsync(ids, ct);

        // سند باید مال همین واحد (هدر معتبر) و هنوز «موقت» باشد؛ وگرنه کل دسته رد می‌شود.
        foreach (var id in ids)
        {
            var row = rows.FirstOrDefault(r => r.ExecutionId == id);
            if (row is null || row.VahedCode != c.VahedCode) throw new NotFoundException("OperationExecution", id);
            if (row.Life != DocLife.Temporary)
                throw new BusinessRuleException($"سند شمارهٔ {row.DocNum} در وضعیت «موقت» نیست و قابل بررسی نیست.");
        }

        // همان تغییر وضعیت کارتابل: گارد G1 و ردیف تاریخچه (Approved) در قلاب همان تراکنش است.
        await _mediator.Send(new ChangeVoucherStateCommand(rows.Select(r => r.VoucherId).ToList(), DocLife.Reviewed), ct);
        return new(rows.Count);
    }
}

// ─────────── برگشت با دلیل ───────────

public sealed record ReturnReviewInput(string ReasonCode, string? Message);

public sealed record ReturnReviewCommand(Guid ExecutionId, string ReasonCode, string? Message) : IRequest<ReviewActionResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ReturnReviewValidator : AbstractValidator<ReturnReviewCommand>
{
    public ReturnReviewValidator()
    {
        RuleFor(x => x.ReasonCode).NotEmpty().WithMessage("کد دلیل برگشت الزامی است.")
            .MaximumLength(30).WithMessage("کد دلیل حداکثر ۳۰ نویسه است.");
        RuleFor(x => x.Message).MaximumLength(500).WithMessage("پیام حداکثر ۵۰۰ نویسه است.");
        RuleFor(x => x.Message).NotEmpty().When(x => string.Equals(x.ReasonCode, "other", StringComparison.OrdinalIgnoreCase))
            .WithMessage("برای دلیل «سایر» نوشتن پیام الزامی است.");
    }
}

public sealed class ReturnReviewHandler : IRequestHandler<ReturnReviewCommand, ReviewActionResult>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly IVoucherHeadRepository _heads;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ReturnReviewHandler(IHesabyarAuditRepository repo, IVoucherHeadRepository heads, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _heads = heads; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<ReviewActionResult> Handle(ReturnReviewCommand c, CancellationToken ct)
    {
        // برگرداندن تأیید نیست، پس سندِ خودِ کاربر هم قابل برگشت است (تصمیم صاحب پروژه).
        ReviewAccess.EnsureReviewer(_currentUser);
        var row = (await _repo.GetRowsAsync([c.ExecutionId], ct)).FirstOrDefault();
        if (row is null || row.VahedCode != c.VahedCode) throw new NotFoundException("OperationExecution", c.ExecutionId);

        // حالت را روی ردیف tracked دوباره می‌سنجیم تا بررسی هم‌زمان دو نفر یکدیگر را بی‌صدا نخورند.
        var head = (await _heads.GetManyForUpdateAsync([row.VoucherId], ct)).FirstOrDefault();
        if (head is null || head.ISDELETED == true) throw new NotFoundException("VoucherHead", row.VoucherId);
        if (head.DOCLIFE != DocLife.Temporary)
            throw new BusinessRuleException($"سند شمارهٔ {head.DOC_NUM} دیگر در وضعیت «موقت» نیست و برگشت نمی‌خورد.");

        var now = DateTime.UtcNow;
        head.DOCLIFE = DocLife.Draft; // یادداشت — ثبت‌کننده اصلاح می‌کند و دوباره «موقت» می‌کند
        head.CHANGEUSERID = _currentUser.UserId;
        head.UPDATEDDATE = now;

        var execution = new OperationExecution { Id = row.ExecutionId, VoucherId = row.VoucherId, VahedCode = row.VahedCode };
        await _repo.AddReviewAsync(HesabyarVoucherStateHook.NewReview(
            execution, ReviewAction.Returned, c.ReasonCode.Trim(), string.IsNullOrWhiteSpace(c.Message) ? null : c.Message.Trim(),
            _currentUser.UserId, now), ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return new(1);
    }
}

// ─────────── سندهای من ───────────

public sealed record MineCountsDto(int Returned, int Pending, int Reviewed);

public sealed record MineItemDto(
    Guid ExecutionId, Guid VoucherId, string? DocNum, string? Date, string? TemplateTitle, string Channel, long TotalAmount,
    string Status, DateTime CreatedAtUtc, string PlainText,
    string? ReturnReasonCode, string? ReturnMessage, DateTime? ReturnedAtUtc);

public sealed record MineLatestReturnDto(Guid ExecutionId, Guid VoucherId, string? DocNum, string? ReasonCode, string? Message, DateTime ReturnedAtUtc);

public sealed record MineDto(
    MineCountsDto Counts, DateTime? OldestPendingAtUtc, MineLatestReturnDto? LatestReturn, IReadOnlyList<MineItemDto> Items);

/// <param name="Status"><c>returned</c> | <c>pending</c> | <c>reviewed</c> | خالی = همه (شمارنده‌ها همیشه کلی‌اند).</param>
public sealed record ListMyVouchersQuery(string? Status) : IRequest<MineDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed class ListMyVouchersHandler : IRequestHandler<ListMyVouchersQuery, MineDto>
{
    private const int MaxRows = 300;
    private readonly IHesabyarAuditRepository _repo;
    private readonly ICurrentUser _currentUser;

    public ListMyVouchersHandler(IHesabyarAuditRepository repo, ICurrentUser currentUser)
    { _repo = repo; _currentUser = currentUser; }

    public async Task<MineDto> Handle(ListMyVouchersQuery q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q.VahedCode) || string.IsNullOrWhiteSpace(_currentUser.UserId))
            return new(new(0, 0, 0), null, null, []);

        var rows = await _repo.ListMineAsync(_currentUser.UserId, q.VahedCode, q.Year, MaxRows, ct);
        var latest = await _repo.LatestReviewsAsync(rows.Select(r => r.ExecutionId).ToList(), ct);
        var all = rows.Select(r =>
        {
            latest.TryGetValue(r.ExecutionId, out var review);
            var status = ReviewAccess.Status(r.Life, review);
            var returned = status == "returned" ? review : null;
            return (Row: r, Status: status, Returned: returned);
        }).ToList();

        var returnedItems = all.Where(x => x.Status == "returned").ToList();
        var latestReturn = returnedItems.OrderByDescending(x => x.Returned!.CreatedAtUtc).FirstOrDefault();
        var pending = all.Where(x => x.Status == "pending").ToList();

        var filter = q.Status?.Trim().ToLowerInvariant();
        var shown = filter is "returned" or "pending" or "reviewed" ? all.Where(x => x.Status == filter) : all;

        return new(
            new(returnedItems.Count, pending.Count, all.Count(x => x.Status == "reviewed")),
            pending.Count == 0 ? null : pending.Min(x => x.Row.CreatedAtUtc),
            latestReturn.Row is null ? null
                : new MineLatestReturnDto(latestReturn.Row.ExecutionId, latestReturn.Row.VoucherId, latestReturn.Row.DocNum,
                    latestReturn.Returned!.ReasonCode, latestReturn.Returned.Message, latestReturn.Returned.CreatedAtUtc),
            shown.Select(x => new MineItemDto(
                x.Row.ExecutionId, x.Row.VoucherId, x.Row.DocNum, x.Row.DateDoc, x.Row.TemplateTitle, x.Row.Channel, x.Row.Amount,
                x.Status, x.Row.CreatedAtUtc, x.Row.Description ?? string.Empty,
                x.Returned?.ReasonCode, x.Returned?.Message, x.Returned?.CreatedAtUtc)).ToList());
    }
}

// ═══════════════════════ ردپای هوش مصنوعی (G4) ═══════════════════════

public sealed record AiTraceDto(
    Guid Id, Guid? ExecutionId, string VahedCode, string CreatedBy, DateTime CreatedAtUtc, string MaskedSentence,
    string Provider, string? Model, string PromptVersion, string? ResultJson, double? Confidence, int LatencyMs,
    bool Succeeded, string? Error);

/// <summary>فقط نقش حسابرسی (<see cref="AppRoles.Audit"/> یا جدول دسترسی نقش‌ها، مثل لاگ تغییرات). <paramref name="ExecutionId"/> خالی = آخرین ۱۰۰ ردپای واحد.</summary>
public sealed record ListAiTracesQuery(Guid? ExecutionId) : IRequest<IReadOnlyList<AiTraceDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListAiTracesHandler : IRequestHandler<ListAiTracesQuery, IReadOnlyList<AiTraceDto>>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly ICurrentUser _currentUser;
    private readonly IRoleMenuAccessStore? _accessStore;

    public ListAiTracesHandler(IHesabyarAuditRepository repo, ICurrentUser currentUser, IRoleMenuAccessStore? accessStore = null)
    { _repo = repo; _currentUser = currentUser; _accessStore = accessStore; }

    public async Task<IReadOnlyList<AiTraceDto>> Handle(ListAiTracesQuery q, CancellationToken ct)
    {
        var snapshot = _accessStore is null ? null : await _accessStore.GetSnapshotAsync(ct);
        var can = snapshot is not null ? snapshot.CanViewAuditLog(_currentUser.AllRoles) : _currentUser.IsInRole(AppRoles.Audit);
        if (!can) throw new RoleAccessDeniedException("مشاهدهٔ ردپای هوش مصنوعی فقط برای نقش حسابرسی (مشاهده لاگ تغییرات) مجاز است.");
        if (string.IsNullOrWhiteSpace(q.VahedCode)) return [];

        var rows = await _repo.ListTracesAsync(q.VahedCode, q.ExecutionId, 100, ct);
        return rows.Select(r => new AiTraceDto(
            r.Trace.Id, r.ExecutionId, r.Trace.VahedCode, r.Trace.CreatedBy, r.Trace.CreatedAtUtc, r.Trace.MaskedSentence,
            r.Trace.Provider, r.Trace.Model, r.Trace.PromptVersion, r.Trace.ResultJson, r.Trace.Confidence, r.Trace.LatencyMs,
            r.Trace.Succeeded, r.Trace.Error)).ToList();
    }
}
