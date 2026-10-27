using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using FluentValidation;
using MediatR;

namespace Accounting.Application.OperationTemplates.Agent;

// ═══════════════════════ حسابیار فاز ۶۵ — گفتگو با دستیار (Orchestrator) ═══════════════════════
//
// هر پیام کاربر: تاریخچهٔ گفتگو + فهرست الگوهای مجاز واحد ⇒ مدل؛ مدل ابزار خواندنی/پیش‌نمایش صدا می‌زند تا
// پاسخ نهایی. آخرین پیش‌نمایش موفق «پیشنهاد سند» است که فرانت با «بررسی و ثبت» در صفحهٔ حسابیار باز می‌کند؛
// ثبت همیشه با کلیک کاربر و همان مسیر Execute (با کنترل ریسک، تفکیک وظایف و پیوست) است.

public interface IAgentConversationRepository
{
    /// <summary>گفتگو (tracked) یا null.</summary>
    Task<AgentConversation?> FindAsync(Guid id, CancellationToken ct);

    Task AddConversationAsync(AgentConversation conversation, CancellationToken ct);

    /// <summary>همهٔ پیام‌های گفتگو به ترتیب SEQ.</summary>
    Task<IReadOnlyList<AgentMessage>> ListMessagesAsync(Guid conversationId, CancellationToken ct);

    Task AddMessageAsync(AgentMessage message, CancellationToken ct);

    /// <summary>گفتگوهای حذف‌نشدهٔ کاربر در واحد، جدیدترین اول.</summary>
    Task<IReadOnlyList<AgentConversation>> ListMineAsync(string userId, string vahedCode, int take, CancellationToken ct);
}

public sealed record AgentChatResultDto(
    bool Enabled,
    string? Provider,
    Guid? ConversationId,
    bool Succeeded,
    string Reply,
    AgentProposalDto? Proposal,
    IReadOnlyList<AgentStepDto> Steps,
    bool SuggestManual = false)
{
    public static AgentChatResultDto Disabled() =>
        new(false, null, null, false, "گفتگو با حسابیار در تنظیمات سرور خاموش است؛ از «ثبت سند با حسابیار» استفاده کنید.", null, []);
}

/// <summary>تشخیص ادعای ثبت/پیش‌نویس در پاسخی که پیش‌نمایش موفق پشتش نیست.</summary>
public static class AgentReplyGuard
{
    public const string Replacement =
        "پیش‌نویس سند ساخته نشد و چیزی ثبت نشده است. لطفاً با «ادامه در ثبت سند با حسابیار» همین کار را آنجا انجام دهید.";

    // فقط ادعای موفقیت؛ سؤال عادی («مبلغ چقدر است؟»، «با چه تاریخی ثبت شود؟») نباید جایگزین شود.
    private static readonly string[] Claims =
        ["ثبت شد", "ثبت شده", "ثبت خواهد", "ثبت می شود", "ثبت میشود", "بررسی و ثبت", "کارت را", "کارت زیر", "کارت پایین",
         "موفقیت", "آماده است", "ساخته شد"];

    public static bool ClaimsVoucher(string reply)
    {
        // ی/ک عربی ⇒ فارسی، نیم‌فاصله ⇒ فاصله
        var text = reply.Replace('ي', 'ی').Replace('ك', 'ک').Replace('‌', ' ');
        return Claims.Any(c => text.Contains(c, StringComparison.Ordinal));
    }
}

// ─────────── ارسال پیام ───────────

public sealed record SendAgentMessageCommand(Guid? ConversationId, string Message) : IRequest<AgentChatResultDto>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed class SendAgentMessageValidator : AbstractValidator<SendAgentMessageCommand>
{
    public const int MaxLength = 1000;

    public SendAgentMessageValidator() =>
        RuleFor(x => x.Message).NotEmpty().WithMessage("پیام خالی است.")
            .MaximumLength(MaxLength).WithMessage($"پیام حداکثر {MaxLength} نویسه است.");
}

public sealed class SendAgentMessageHandler : IRequestHandler<SendAgentMessageCommand, AgentChatResultDto>
{
    private const string GiveUpReply = "نتوانستم کار را کامل کنم؛ لطفاً ساده‌تر بگویید یا از «ثبت سند با حسابیار» استفاده کنید.";
    private const string UnavailableReply = "سرویس هوش مصنوعی الان در دسترس نیست؛ کمی بعد دوباره بفرستید یا از «ثبت سند با حسابیار» استفاده کنید.";
    private static readonly JsonSerializerOptions Json = AgentJson.Options;

    private readonly IAgentChatModel _model;
    private readonly IAgentConversationRepository _conversations;
    private readonly IOperationTemplateRepository _templates;
    private readonly AssistantUnitPolicy _policy;
    private readonly AgentToolbox _toolbox;
    private readonly IHesabyarAuditRepository _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly AssistantAgentOptions _options;

    public SendAgentMessageHandler(IAgentChatModel model, IAgentConversationRepository conversations, IOperationTemplateRepository templates,
        AssistantUnitPolicy policy, AgentToolbox toolbox, IHesabyarAuditRepository audit, IUnitOfWork unitOfWork, ICurrentUser currentUser,
        AssistantAgentOptions? options = null)
    {
        _model = model;
        _conversations = conversations;
        _templates = templates;
        _policy = policy;
        _toolbox = toolbox;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _options = options ?? new AssistantAgentOptions();
    }

    public async Task<AgentChatResultDto> Handle(SendAgentMessageCommand c, CancellationToken ct)
    {
        if (!_model.IsEnabled || !_options.Enabled) return AgentChatResultDto.Disabled();
        if (string.IsNullOrWhiteSpace(c.VahedCode))
            throw new BusinessRuleException("کاربر به هیچ واحدی متصل نیست؛ گفتگو با حسابیار فقط در سطح واحد ممکن است.");

        var message = c.Message.Trim();
        var now = DateTime.UtcNow;

        AgentConversation conversation;
        IReadOnlyList<AgentMessage> history = [];
        if (c.ConversationId is { } id)
        {
            conversation = await AgentConversationAccess.LoadAsync(_conversations, id, c.VahedCode, _currentUser, ct);
            history = await _conversations.ListMessagesAsync(id, ct);
        }
        else
        {
            conversation = new AgentConversation
            {
                Id = Guid.NewGuid(),
                VahedCode = c.VahedCode,
                Title = message.Length <= 60 ? message : message[..60] + "…",
                CreatedBy = _currentUser.UserId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            await _conversations.AddConversationAsync(conversation, ct);
        }

        var templates = await _policy.FilterAsync(await _templates.ListActiveWithParametersAsync(ct), c.VahedCode, ct);
        var ctx = new AgentToolContext
        {
            VahedCode = c.VahedCode,
            Year = c.Year,
            Templates = templates,
            DataTools = !_model.IsExternal || _options.AllowDataToolsForExternal,
        };

        // G5: یک جلسهٔ پوشاندن برای کل درخواست (تاریخچه، پیام، نتیجهٔ ابزارها)؛ پاسخ مدل در سرور برگردانده می‌شود.
        var mask = _model.RequiresPiiMasking ? new PiiMaskSession() : null;
        string M(string? s) => mask?.Mask(s) ?? s ?? "";
        string R(string? s) => mask?.Restore(s) ?? s ?? "";

        var turns = new List<AgentTurn>();
        foreach (var h in history.Where(h => h.Role != AgentMessageRole.Tool).TakeLast(Math.Clamp(_options.HistoryMessages, 0, 50)))
            turns.Add(new AgentTurn(h.Role == AgentMessageRole.User ? AgentTurnRole.User : AgentTurnRole.Assistant,
                M(h.Role == AgentMessageRole.Assistant ? WithProposalNote(h) : h.Content)));
        // مدل‌ها گفتگوی شروع‌شده با «دستیار» را نمی‌پذیرند
        while (turns.Count > 0 && turns[0].Role != AgentTurnRole.User) turns.RemoveAt(0);
        turns.Add(new AgentTurn(AgentTurnRole.User, M(message)));

        var request = new AgentModelRequest(AgentPromptBuilder.Build(templates, c.Year, ctx.DataTools), turns, AgentToolbox.Specs(ctx.DataTools));
        var toolMessages = new List<(string Name, string Content)>();
        string? reply = null;
        string? error = null;
        var clock = Stopwatch.StartNew();
        try
        {
            for (var round = 0; round <= Math.Clamp(_options.MaxToolRounds, 1, 12); round++)
            {
                var answer = await _model.CompleteAsync(request with { Turns = turns }, ct);
                if (answer.ToolCalls.Count == 0)
                {
                    reply = answer.Text;
                    break;
                }
                turns.Add(new AgentTurn(AgentTurnRole.Assistant, answer.Text, answer.ToolCalls));
                foreach (var call in answer.ToolCalls)
                {
                    var (result, step) = await _toolbox.RunAsync(call with { ArgumentsJson = R(call.ArgumentsJson) }, ctx, ct);
                    ctx.Steps.Add(step);
                    turns.Add(new AgentTurn(AgentTurnRole.Tool, M(result), ToolCallId: call.Id, ToolName: call.Name));

                    // ردپای ابزار همیشه پوشانده ذخیره می‌شود (حتی برای مدل داخلی).
                    var storedArgs = PiiMasker.Mask(R(call.ArgumentsJson)).Masked;
                    var storedResult = PiiMasker.Mask(result).Masked;
                    toolMessages.Add((call.Name, Limit(
                        $"{{\"args\":{(string.IsNullOrWhiteSpace(storedArgs) ? "{}" : storedArgs)},\"result\":{storedResult}}}", 4000)));
                }
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            error = "timeout";
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or JsonException or KeyNotFoundException)
        {
            // جزئیات خطای سرویس بیرونی نه به کاربر می‌رود و نه ذخیره می‌شود؛ فقط نوع خطا.
            error = ex.GetType().Name;
        }
        clock.Stop();

        var succeeded = error is null && !string.IsNullOrWhiteSpace(reply);
        var finalText = error is not null ? UnavailableReply : string.IsNullOrWhiteSpace(reply) ? GiveUpReply : R(reply).Trim();

        // گارد: مدل بی‌پیش‌نویس واقعی از ثبت/کارت حرف زد (مدل‌های کوچک دستور را رعایت نمی‌کنند) ⇒ جملهٔ گمراه‌کننده جایگزین می‌شود.
        var guarded = ctx.Proposal is null && succeeded && AgentReplyGuard.ClaimsVoucher(finalText);
        if (guarded) finalText = AgentReplyGuard.Replacement;

        var trace = new OperationAiTrace
        {
            Id = Guid.NewGuid(),
            VahedCode = c.VahedCode,
            MaskedSentence = Limit(PiiMasker.Mask(message).Masked, 1000),
            Provider = _model.ProviderName,
            Model = _model.ModelName,
            PromptVersion = AgentPromptBuilder.PromptVersion,
            ResultJson = Limit(PiiMasker.Mask(JsonSerializer.Serialize(new
            {
                reply = finalText,
                steps = ctx.Steps.Select(s => s.Tool),
                proposal = ctx.Proposal is null ? null : new { ctx.Proposal.TemplateCode, ctx.Proposal.VoucherDate, answers = ctx.Proposal.Answers.Select(a => new { a.Key, a.Value }) },
            }, Json)).Masked, 20000),
            LatencyMs = (int)Math.Min(clock.ElapsedMilliseconds, int.MaxValue),
            Succeeded = succeeded,
            Error = error ?? (succeeded ? null : "no-reply"),
            CreatedBy = _currentUser.UserId,
            CreatedAtUtc = now,
        };
        await _audit.AddTraceAsync(trace, ct);

        var proposal = ctx.Proposal is null ? null : ctx.Proposal with { AiTraceId = trace.Id };
        var seq = history.Count == 0 ? 0 : history.Max(h => h.Seq);
        await _conversations.AddMessageAsync(Message(conversation.Id, ++seq, AgentMessageRole.User, message, now), ct);
        foreach (var (name, content) in toolMessages)
        {
            var tool = Message(conversation.Id, ++seq, AgentMessageRole.Tool, content, now);
            tool.ToolName = name;
            await _conversations.AddMessageAsync(tool, ct);
        }
        var assistant = Message(conversation.Id, ++seq, AgentMessageRole.Assistant, finalText, now);
        assistant.ProposalJson = proposal is null ? null : JsonSerializer.Serialize(proposal, Json);
        assistant.AiTraceId = trace.Id;
        await _conversations.AddMessageAsync(assistant, ct);
        conversation.UpdatedAtUtc = now;

        await _unitOfWork.SaveChangesAsync(ct);
        return new AgentChatResultDto(true, _model.ProviderName, conversation.Id, succeeded, finalText, proposal, ctx.Steps,
            SuggestManual: proposal is null && (guarded || !succeeded));
    }

    private static AgentMessage Message(Guid conversationId, int seq, AgentMessageRole role, string content, DateTime now) => new()
    {
        Id = Guid.NewGuid(), ConversationId = conversationId, Seq = seq, Role = role, Content = content, CreatedAtUtc = now,
    };

    /// <summary>پاسخ قبلی دستیار + خلاصهٔ پیشنهاد سندش، تا مدل بداند چه جواب‌هایی قبلاً داده شده.</summary>
    private static string WithProposalNote(AgentMessage m)
    {
        if (string.IsNullOrWhiteSpace(m.ProposalJson)) return m.Content;
        try
        {
            var p = JsonSerializer.Deserialize<AgentProposalDto>(m.ProposalJson, Json);
            if (p is null) return m.Content;
            var answers = string.Join("; ", p.Answers.Select(a => $"{a.Key}={a.Value}"));
            return $"{m.Content}\n[previewed template {p.TemplateCode}, voucher_date {p.VoucherDate}, answers: {answers}]";
        }
        catch (JsonException)
        {
            return m.Content;
        }
    }

    private static string Limit(string s, int max) => s.Length <= max ? s : s[..max];
}

internal static class AgentConversationAccess
{
    /// <summary>گفتگوی همین کاربر در همین واحد؛ وگرنه ۴۰۴ (گفتگوی دیگری دیده نمی‌شود).</summary>
    public static async Task<AgentConversation> LoadAsync(IAgentConversationRepository repo, Guid id, string vahedCode, ICurrentUser user, CancellationToken ct)
    {
        var c = await repo.FindAsync(id, ct);
        if (c is null || c.IsDeleted || c.VahedCode != vahedCode || !string.Equals(c.CreatedBy, user.UserId, StringComparison.Ordinal))
            throw new NotFoundException("AgentConversation", id);
        return c;
    }
}

/// <summary>دستور سیستم دستیار گفتگو.</summary>
public static class AgentPromptBuilder
{
    /// <summary>نسخهٔ پرامپت برای ردپای هوش مصنوعی (G4) — با هر تغییر متن دستور یا ابزارها بالا ببر.</summary>
    public const string PromptVersion = "agent-v1";

    public static string Build(IReadOnlyList<OperationTemplate> templates, string? fiscalYear, bool dataTools)
    {
        var s = new StringBuilder();
        s.AppendLine("You are «حسابیار», the assistant of a Persian accounting system used by non-accountant staff of a government organization.");
        s.AppendLine("You help them record accounting vouchers by choosing ONE operation template from the catalog and filling its parameters, and you answer simple balance questions.");
        s.AppendLine("Rules:");
        s.AppendLine("- Always reply in Persian (Farsi): short, friendly, plain words. Never show ids, codes of templates, JSON or tool names to the user.");
        s.AppendLine("- You can NOT post (register) a voucher and you must never say it is registered. To prepare one, call preview_voucher with every answer you have.");
        s.AppendLine("- When preview_voucher succeeds, say in one or two sentences what will be recorded and ask the user to check the card below and press «بررسی و ثبت».");
        s.AppendLine("- Choose templates only from the catalog. If none fits, say so and suggest «سند دستی» or sending the request to the accountant. If two fit, ask one short question.");
        s.AppendLine("- Ask for missing required parameters ONE at a time, using that parameter's question text. Never invent values; only use what the user said.");
        s.AppendLine("- If preview_voucher returns errors, ask the user about the parameter named in the error (use its «ask» text). After any change, call preview_voucher again with all answers.");
        s.AppendLine("- Amount: integer Rials, Latin digits. «تومان/تومن» = ×10 Rials. «هزار» = 1,000, «میلیون» = 1,000,000, «میلیارد» = 1,000,000,000.");
        s.AppendLine($"- Dates: Jalali yyyyMMdd with Latin digits. Today is {IntentPromptBuilder.TodayJalali()}. «دیروز» = today − 1 day.");
        if (!string.IsNullOrWhiteSpace(fiscalYear))
            s.AppendLine($"- The user's fiscal year is {fiscalYear}; a date without a year belongs to that year.");
        s.AppendLine("- Choice parameters: answer with the option value; «نمی‌دانم» = unknown.");
        if (dataTools)
        {
            s.AppendLine("- Detail parameters (طرف حساب): call search_counterparty with the name the user gave. Exactly one result ⇒ use its id as the value. Several ⇒ list their titles (at most 5) and ask which one. None ⇒ ask the user to check the name.");
            s.AppendLine("- Balance questions: use get_account_balance or get_counterparty_balance. Say the amount in Rials with thousand separators and whether it is بدهکار or بستانکار.");
        }
        else
        {
            s.AppendLine("- Detail parameters (طرف حساب): put the name exactly as the user wrote it as the value; the system finds it. If preview says it is not found or not unique, ask the user for the full name.");
            s.AppendLine("- Balances are not available in this mode; say so briefly and point to the reports menu.");
        }
        s.AppendLine("- Placeholders like [[NID_1]], [[IBAN_1]], [[CARD_1]], [[TEL_1]] are masked private data: never treat them as amounts or dates; copy them unchanged if needed.");
        s.AppendLine("- Do not answer questions unrelated to this accounting system.");
        s.AppendLine();
        IntentPromptBuilder.AppendCatalog(s, templates);
        return s.ToString();
    }
}

// ─────────── وضعیت، فهرست، گفتگو، حذف ───────────

/// <param name="DataTools">جستجوی طرف حساب و مانده برای دستیار فعال است.</param>
public sealed record AgentStatusDto(bool Enabled, string? Provider, string? Model, bool DataTools);

public sealed record GetAgentStatusQuery : IRequest<AgentStatusDto>;

public sealed class GetAgentStatusHandler : IRequestHandler<GetAgentStatusQuery, AgentStatusDto>
{
    private readonly IAgentChatModel _model;
    private readonly AssistantAgentOptions _options;

    public GetAgentStatusHandler(IAgentChatModel model, AssistantAgentOptions? options = null)
    {
        _model = model;
        _options = options ?? new AssistantAgentOptions();
    }

    public Task<AgentStatusDto> Handle(GetAgentStatusQuery q, CancellationToken ct) =>
        Task.FromResult(_model.IsEnabled && _options.Enabled
            ? new AgentStatusDto(true, _model.ProviderName, _model.ModelName, !_model.IsExternal || _options.AllowDataToolsForExternal)
            : new AgentStatusDto(false, null, null, false));
}

public sealed record AgentConversationSummaryDto(Guid Id, string Title, DateTime UpdatedAtUtc);

public sealed record ListAgentConversationsQuery : IRequest<IReadOnlyList<AgentConversationSummaryDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class ListAgentConversationsHandler : IRequestHandler<ListAgentConversationsQuery, IReadOnlyList<AgentConversationSummaryDto>>
{
    private readonly IAgentConversationRepository _repo;
    private readonly ICurrentUser _currentUser;

    public ListAgentConversationsHandler(IAgentConversationRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AgentConversationSummaryDto>> Handle(ListAgentConversationsQuery q, CancellationToken ct) =>
        (await _repo.ListMineAsync(_currentUser.UserId, q.VahedCode, 30, ct))
        .Select(c => new AgentConversationSummaryDto(c.Id, c.Title, c.UpdatedAtUtc)).ToList();
}

/// <param name="Role"><c>user</c> یا <c>assistant</c>.</param>
public sealed record AgentMessageDto(string Role, string Text, AgentProposalDto? Proposal, DateTime CreatedAtUtc);

public sealed record AgentConversationDto(Guid Id, string Title, IReadOnlyList<AgentMessageDto> Messages);

public sealed record GetAgentConversationQuery(Guid Id) : IRequest<AgentConversationDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetAgentConversationHandler : IRequestHandler<GetAgentConversationQuery, AgentConversationDto>
{
    private static readonly JsonSerializerOptions Json = AgentJson.Options;
    private readonly IAgentConversationRepository _repo;
    private readonly ICurrentUser _currentUser;

    public GetAgentConversationHandler(IAgentConversationRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<AgentConversationDto> Handle(GetAgentConversationQuery q, CancellationToken ct)
    {
        var c = await AgentConversationAccess.LoadAsync(_repo, q.Id, q.VahedCode, _currentUser, ct);
        var messages = (await _repo.ListMessagesAsync(c.Id, ct))
            .Where(m => m.Role != AgentMessageRole.Tool)
            .Select(m => new AgentMessageDto(m.Role == AgentMessageRole.User ? "user" : "assistant", m.Content, Proposal(m.ProposalJson), m.CreatedAtUtc))
            .ToList();
        return new AgentConversationDto(c.Id, c.Title, messages);
    }

    private static AgentProposalDto? Proposal(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<AgentProposalDto>(json, Json); }
        catch (JsonException) { return null; }
    }
}

public sealed record DeleteAgentConversationCommand(Guid Id) : IRequest<Unit>, IVahedScopedCommand
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class DeleteAgentConversationHandler : IRequestHandler<DeleteAgentConversationCommand, Unit>
{
    private readonly IAgentConversationRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteAgentConversationHandler(IAgentConversationRepository repo, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteAgentConversationCommand c, CancellationToken ct)
    {
        var conversation = await AgentConversationAccess.LoadAsync(_repo, c.Id, c.VahedCode, _currentUser, ct);
        conversation.IsDeleted = true;
        conversation.UpdatedAtUtc = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
