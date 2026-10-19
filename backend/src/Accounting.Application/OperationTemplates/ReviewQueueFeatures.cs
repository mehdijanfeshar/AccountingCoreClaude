using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.OperationTemplates;

// ═══════════════════════ فاز ۶۲ — صف بررسی با ریسک، پاسخ به حسابدار، درخواست‌ها، پیشنهاد جواب (DDL 084) ═══════════════════════

// ─────────── صف بررسی ───────────

public sealed record ReviewOriginDto(string? TemplateTitle, IReadOnlyList<string> AiFilledKeys, bool UserConfirmedAll);

public sealed record ReviewReplyDto(string Message, DateTime CreatedAtUtc);

/// <param name="RiskLevel"><c>low</c> | <c>medium</c> | <c>high</c> (ردیف قدیمی بدون ریسک ثبت‌شده = <c>low</c>).</param>
public sealed record ReviewItemDto(
    Guid ExecutionId, Guid VoucherId, string? DocNum, string? Date, string? TemplateTitle, string Channel,
    long TotalAmount, string CreatedBy, DateTime CreatedAtUtc, bool IsMine, string PlainText, IReadOnlyList<string> Warnings,
    string RiskLevel = "low", IReadOnlyList<string>? RiskReasons = null, string? UserSentence = null,
    ReviewOriginDto? Origin = null, IReadOnlyList<ControlDto>? Controls = null, ReviewReplyDto? LatestReply = null);

public sealed record ReviewCountsDto(int All, int Low, int Attention);

public sealed record ReviewQueuePageDto(IReadOnlyList<ReviewItemDto> Items, int Total, int Page, int PageSize, ReviewCountsDto? Counts = null);

internal static class ReviewItemMapper
{
    public static ReviewItemDto ToDto(ReviewVoucherRow r, ICurrentUser user, OperationReview? reply)
    {
        var isMine = string.Equals(r.CreatedBy, user.UserId, StringComparison.Ordinal);
        var sources = RiskText.ParseSources(r.AnswerSourcesJson);
        var aiKeys = sources.Where(kv => string.Equals(kv.Value, "ai", StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();

        var controls = new List<ControlDto>
        {
            new("balanced", "دو طرف سند برابر است", true),
            new("notCreator", "ثبت‌کننده شما نیستید", !isMine),
        };
        if (r.RequireAttachment) controls.Add(new("attachment", "مدرک پیوست شده است", r.HasAttachment));
        if (r.Risk == RiskLevel.High) controls.Add(new("financeManager", "تأیید مدیر مالی (ریسک بالا)", user.IsInRole(AppRoles.MaliAdmin)));

        return new ReviewItemDto(
            r.ExecutionId, r.VoucherId, r.DocNum, r.DateDoc, r.TemplateTitle, r.Channel, r.Amount, r.CreatedBy, r.CreatedAtUtc,
            isMine, r.Description ?? string.Empty, RiskText.ParseList(r.WarningsJson),
            RiskText.Level(r.Risk), RiskText.ParseList(r.RiskReasonsJson), r.UserSentence,
            new ReviewOriginDto(r.TemplateTitle, aiKeys, aiKeys.Count == 0 || r.AiConfirmed == true),
            controls,
            reply is null ? null : new ReviewReplyDto(reply.Message ?? string.Empty, reply.CreatedAtUtc));
    }
}

/// <param name="Risk"><c>low</c> | <c>attention</c> (= متوسط و بالا) | خالی/<c>all</c> = همه.</param>
public sealed record ListReviewQueueQuery(string? Risk, int Page = 1, int PageSize = 20) : IRequest<ReviewQueuePageDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed class ListReviewQueueHandler : IRequestHandler<ListReviewQueueQuery, ReviewQueuePageDto>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly ICurrentUser _currentUser;

    public ListReviewQueueHandler(IHesabyarAuditRepository repo, ICurrentUser currentUser)
    { _repo = repo; _currentUser = currentUser; }

    public async Task<ReviewQueuePageDto> Handle(ListReviewQueueQuery q, CancellationToken ct)
    {
        ReviewAccess.EnsureReviewer(_currentUser);
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 5, 100);
        if (string.IsNullOrWhiteSpace(q.VahedCode)) return new([], 0, page, size, new(0, 0, 0));

        var risk = q.Risk?.Trim().ToLowerInvariant();
        risk = risk is "low" or "attention" ? risk : null;

        var (rows, total) = await _repo.ListByLifeAsync(q.VahedCode, DocLife.Temporary, q.Year, (page - 1) * size, size, ct, risk);
        var counts = await _repo.CountByRiskAsync(q.VahedCode, DocLife.Temporary, q.Year, ct);
        var replies = await _repo.LatestRepliesAsync(rows.Select(r => r.ExecutionId).ToList(), ct);
        return new(rows.Select(r => ReviewItemMapper.ToDto(r, _currentUser, replies.GetValueOrDefault(r.ExecutionId))).ToList(),
            total, page, size, new(counts.All, counts.Low, counts.Attention));
    }
}

// ─────────── جزئیات یک سند (برای باز کردن کارت) ───────────

public sealed record ReviewHistoryDto(string Action, string? ReasonCode, string? Message, string ReviewerId, DateTime CreatedAtUtc);

public sealed record ReviewDetailDto(ReviewItemDto Item, string Status, IReadOnlyList<ReviewHistoryDto> History);

public sealed record GetReviewDetailQuery(Guid ExecutionId) : IRequest<ReviewDetailDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetReviewDetailHandler : IRequestHandler<GetReviewDetailQuery, ReviewDetailDto>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly ICurrentUser _currentUser;

    public GetReviewDetailHandler(IHesabyarAuditRepository repo, ICurrentUser currentUser)
    { _repo = repo; _currentUser = currentUser; }

    public async Task<ReviewDetailDto> Handle(GetReviewDetailQuery q, CancellationToken ct)
    {
        ReviewAccess.EnsureReviewer(_currentUser);
        var row = (await _repo.GetRowsAsync([q.ExecutionId], ct)).FirstOrDefault();
        if (row is null || row.VahedCode != q.VahedCode) throw new NotFoundException("OperationExecution", q.ExecutionId);

        var history = await _repo.ListReviewsAsync(q.ExecutionId, ct);
        var latestState = history.LastOrDefault(h => h.Action != ReviewAction.Replied);
        var reply = history.LastOrDefault(h => h.Action == ReviewAction.Replied);
        return new(ReviewItemMapper.ToDto(row, _currentUser, reply), ReviewAccess.Status(row.Life, latestState),
            history.Select(h => new ReviewHistoryDto(h.Action.ToString().ToLowerInvariant(), h.ReasonCode, h.Message, h.ReviewerId, h.CreatedAtUtc)).ToList());
    }
}

// ─────────── «پاسخ به حسابدار» (فقط ثبت‌کنندهٔ سند) ───────────

public sealed record ReplyInput(string Message);

public sealed record ReplyToReviewCommand(Guid ExecutionId, string Message) : IRequest<ReviewActionResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ReplyToReviewValidator : AbstractValidator<ReplyToReviewCommand>
{
    public ReplyToReviewValidator() =>
        RuleFor(x => x.Message).NotEmpty().WithMessage("متن پاسخ الزامی است.")
            .MaximumLength(500).WithMessage("پاسخ حداکثر ۵۰۰ نویسه است.");
}

public sealed class ReplyToReviewHandler : IRequestHandler<ReplyToReviewCommand, ReviewActionResult>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ReplyToReviewHandler(IHesabyarAuditRepository repo, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<ReviewActionResult> Handle(ReplyToReviewCommand c, CancellationToken ct)
    {
        var row = (await _repo.GetRowsAsync([c.ExecutionId], ct)).FirstOrDefault();
        if (row is null || row.VahedCode != c.VahedCode) throw new NotFoundException("OperationExecution", c.ExecutionId);
        if (!string.Equals(row.CreatedBy, _currentUser.UserId, StringComparison.Ordinal))
            throw new RoleAccessDeniedException("فقط ثبت‌کنندهٔ سند می‌تواند به حسابدار پاسخ دهد.");

        var execution = new OperationExecution { Id = row.ExecutionId, VoucherId = row.VoucherId, VahedCode = row.VahedCode };
        await _repo.AddReviewAsync(HesabyarVoucherStateHook.NewReview(
            execution, ReviewAction.Replied, null, c.Message.Trim(), _currentUser.UserId, DateTime.UtcNow), ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return new(1);
    }
}

// ─────────── «موضوع را برای حسابدار بفرستید» ───────────

public sealed record RequestInput(string Sentence);
public sealed record RequestDoneInput(string? Note);

public sealed record OperationRequestDto(
    Guid Id, string Sentence, string CreatedBy, DateTime CreatedAtUtc, string Status,
    string? HandledBy, DateTime? HandledAtUtc, string? Note);

public sealed record CreateOperationRequestResult(Guid Id);

/// <summary>هر کاربر عملیات می‌تواند بفرستد؛ فقط بررسی‌کنندگان واحد می‌بینند و تمام می‌کنند.</summary>
public sealed record CreateOperationRequestCommand(string Sentence) : IRequest<CreateOperationRequestResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class CreateOperationRequestValidator : AbstractValidator<CreateOperationRequestCommand>
{
    public CreateOperationRequestValidator() =>
        RuleFor(x => x.Sentence).NotEmpty().WithMessage("متن درخواست الزامی است.")
            .MaximumLength(1000).WithMessage("متن درخواست حداکثر ۱۰۰۰ نویسه است.");
}

public sealed class CreateOperationRequestHandler : IRequestHandler<CreateOperationRequestCommand, CreateOperationRequestResult>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateOperationRequestHandler(IHesabyarAuditRepository repo, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<CreateOperationRequestResult> Handle(CreateOperationRequestCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.VahedCode))
            throw new BusinessRuleException("کاربر به هیچ واحدی متصل نیست.");
        var request = new OperationRequest
        {
            Id = Guid.NewGuid(),
            VahedCode = c.VahedCode,
            Sentence = c.Sentence.Trim(),
            CreatedBy = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
            Status = RequestStatus.Open,
        };
        await _repo.AddRequestAsync(request, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return new(request.Id);
    }
}

/// <param name="Status"><c>open</c> | <c>done</c> | خالی = همه.</param>
public sealed record ListOperationRequestsQuery(string? Status) : IRequest<IReadOnlyList<OperationRequestDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListOperationRequestsHandler : IRequestHandler<ListOperationRequestsQuery, IReadOnlyList<OperationRequestDto>>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly ICurrentUser _currentUser;

    public ListOperationRequestsHandler(IHesabyarAuditRepository repo, ICurrentUser currentUser)
    { _repo = repo; _currentUser = currentUser; }

    public async Task<IReadOnlyList<OperationRequestDto>> Handle(ListOperationRequestsQuery q, CancellationToken ct)
    {
        ReviewAccess.EnsureReviewer(_currentUser);
        if (string.IsNullOrWhiteSpace(q.VahedCode)) return [];
        RequestStatus? status = q.Status?.Trim().ToLowerInvariant() switch { "open" => RequestStatus.Open, "done" => RequestStatus.Done, _ => null };
        return (await _repo.ListRequestsAsync(q.VahedCode, status, 200, ct))
            .Select(r => new OperationRequestDto(r.Id, r.Sentence, r.CreatedBy, r.CreatedAtUtc,
                r.Status == RequestStatus.Done ? "done" : "open", r.HandledBy, r.HandledAtUtc, r.Note)).ToList();
    }
}

public sealed record MarkRequestDoneCommand(Guid Id, string? Note) : IRequest<ReviewActionResult>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class MarkRequestDoneValidator : AbstractValidator<MarkRequestDoneCommand>
{
    public MarkRequestDoneValidator() =>
        RuleFor(x => x.Note).MaximumLength(500).WithMessage("یادداشت حداکثر ۵۰۰ نویسه است.");
}

public sealed class MarkRequestDoneHandler : IRequestHandler<MarkRequestDoneCommand, ReviewActionResult>
{
    private readonly IHesabyarAuditRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public MarkRequestDoneHandler(IHesabyarAuditRepository repo, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<ReviewActionResult> Handle(MarkRequestDoneCommand c, CancellationToken ct)
    {
        ReviewAccess.EnsureReviewer(_currentUser);
        var request = await _repo.FindRequestForUpdateAsync(c.Id, ct);
        if (request is null || request.VahedCode != c.VahedCode) throw new NotFoundException("OperationRequest", c.Id);
        if (request.Status == RequestStatus.Done) return new(0); // قبلاً انجام شده؛ تکرار بی‌اثر

        request.Status = RequestStatus.Done;
        request.HandledBy = _currentUser.UserId;
        request.HandledAtUtc = DateTime.UtcNow;
        request.Note = string.IsNullOrWhiteSpace(c.Note) ? null : c.Note.Trim();
        await _unitOfWork.SaveChangesAsync(ct);
        return new(1);
    }
}

// ─────────── پیشنهاد جواب: «شرکت … · ۲ بار قبل» ───────────

public sealed record AnswerSuggestionDto(Guid DetailId, string Title, int Count, DateTime LastUsedAtUtc);

public sealed record SuggestAnswersQuery(Guid TemplateId, string ParamKey) : IRequest<IReadOnlyList<AnswerSuggestionDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class SuggestAnswersHandler : IRequestHandler<SuggestAnswersQuery, IReadOnlyList<AnswerSuggestionDto>>
{
    private const int Top = 5;
    private const int Scan = 300;
    private readonly IOperationTemplateRepository _repo;
    private readonly IDetailReader _details;
    private readonly AssistantUnitPolicy _policy;

    public SuggestAnswersHandler(IOperationTemplateRepository repo, IDetailReader details, AssistantUnitPolicy policy)
    { _repo = repo; _details = details; _policy = policy; }

    public async Task<IReadOnlyList<AnswerSuggestionDto>> Handle(SuggestAnswersQuery q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q.VahedCode)) return [];
        var template = await _repo.GetFullAsync(q.TemplateId, ct) ?? throw new NotFoundException("OperationTemplate", q.TemplateId);
        var param = template.Parameters.FirstOrDefault(p => string.Equals(p.Key, q.ParamKey, StringComparison.OrdinalIgnoreCase));
        if (param is null || param.Type != ParameterType.Detail) return [];
        if (await _policy.TemplateNotAllowedAsync(template, q.VahedCode, ct) is not null) return [];

        var (executions, _) = await _repo.ListExecutionsAsync(
            new ExecutionFilter(q.VahedCode, null, q.TemplateId, null, null, null, 0, Scan), ct);

        var counted = executions
            .Select(e => (E: e, Raw: ListMyRecentOperationsHandler.ParseValues(e.InputJson).GetValueOrDefault(param.Key)))
            .Where(x => Guid.TryParse(x.Raw, out _))
            .GroupBy(x => Guid.Parse(x.Raw!))
            .Select(g => (Id: g.Key, Count: g.Count(), Last: g.Max(x => x.E.CreatedAtUtc)))
            .OrderByDescending(x => x.Count).ThenByDescending(x => x.Last)
            .Take(Top * 2) // غیرفعال/غیرمجازها پایین‌تر حذف می‌شوند
            .ToList();
        if (counted.Count == 0) return [];

        var info = await _details.GetByIdsAsync(counted.Select(x => x.Id).ToList(), q.VahedCode, ct);
        return counted
            .Where(x => info.TryGetValue(x.Id, out var d) && d.IsActive && d.IsVisibleToUnit)
            .Take(Top)
            .Select(x => new AnswerSuggestionDto(x.Id, info[x.Id].Title, x.Count, x.Last))
            .ToList();
    }
}
