using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using MediatR;

namespace Accounting.Application.OperationTemplates;

// ═══════════════════════ طراحی الگو (صفحهٔ ستاد) ═══════════════════════
// دیدن تعریف‌ها برای همهٔ نقش‌ها مجاز است (Query)؛ هر تغییر فقط مدیر ستاد.

internal static class TemplateMapping
{
    public static OperationTemplate Build(Guid id, string? code, string? title, string? description,
        string? voucherDescriptionPattern, List<TemplateParameterDto>? parameters, List<TemplateLineDto>? lines, Guid? systemTypeId = null, string? keywords = null,
        IReadOnlyList<string>? allowedVahedTypes = null) => new()
    {
        Id = id,
        SystemTypeId = systemTypeId,
        Keywords = string.IsNullOrWhiteSpace(keywords) ? null : keywords.Trim(),
        AllowedVahedTypes = JoinVahedTypes(allowedVahedTypes),
        Code = code?.Trim().ToUpperInvariant() ?? "",
        Title = title?.Trim() ?? "",
        Description = description?.Trim() ?? "",
        VoucherDescriptionPattern = voucherDescriptionPattern?.Trim() ?? "",
        Parameters = BuildParameters(parameters),
        Lines = BuildLines(lines),
    };

    public static List<TemplateParameter> BuildParameters(List<TemplateParameterDto>? parameters) =>
        (parameters ?? new()).Select(p => new TemplateParameter
        {
            Id = Guid.NewGuid(),
            Key = p.Key?.Trim() ?? "", Title = p.Title?.Trim() ?? "", Type = p.Type, DetailGroupId = p.DetailGroupId,
            IsRequired = p.IsRequired, AskPrompt = p.AskPrompt?.Trim() ?? "", SortOrder = p.SortOrder
        }).ToList();

    public static List<TemplateLine> BuildLines(List<TemplateLineDto>? lines) =>
        (lines ?? new()).Select(l => new TemplateLine
        {
            Id = Guid.NewGuid(),
            Side = l.Side, SubsidiaryAccountId = l.SubsidiaryAccountId,
            AmountParameterKey = l.IsBalancingLine ? "" : l.AmountParameterKey ?? "",
            Percent = l.IsBalancingLine ? 100m : l.Percent,
            IsBalancingLine = l.IsBalancingLine,
            DescriptionPattern = string.IsNullOrWhiteSpace(l.DescriptionPattern) ? null : l.DescriptionPattern.Trim(),
            SortOrder = l.SortOrder,
            Details = (l.Details ?? new()).Select(d => new TemplateLineDetail
            {
                Id = Guid.NewGuid(), Level = d.Level,
                ParameterKey = string.IsNullOrWhiteSpace(d.ParameterKey) ? null : d.ParameterKey.Trim(),
                FixedDetailId = d.FixedDetailId
            }).ToList()
        }).ToList();

    /// <summary>کدهای نوع واحد ⇒ «17,21»؛ خالی ⇒ null (همهٔ واحدها).</summary>
    public static string? JoinVahedTypes(IReadOnlyList<string>? codes)
    {
        var list = (codes ?? []).Select(c => c?.Trim() ?? "").Where(c => c.Length > 0).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList();
        return list.Count == 0 ? null : string.Join(",", list);
    }

    public static IReadOnlyList<string> SplitVahedTypes(string? stored) =>
        string.IsNullOrWhiteSpace(stored) ? [] : stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static void EnsureSetad(ICurrentUser user)
    {
        if (!user.IsInRole(AppRoles.SetadAdmin))
            throw new RoleAccessDeniedException("تعریف و تغییر الگوی عملیات فقط با نقش «مدیر ستاد» ممکن است.");
    }
}

// ─────────── فهرست همهٔ الگوها (فعال و غیرفعال) ───────────

public sealed record ListOperationTemplateDefinitionsQuery : IRequest<IReadOnlyList<TemplateDefinitionSummaryDto>>;

public sealed record TemplateDefinitionSummaryDto(Guid Id, string Code, string Title, string Description,
    bool IsActive, int ParameterCount, int LineCount, string CreatedBy, DateTime CreatedAtUtc);

public sealed class ListOperationTemplateDefinitionsHandler
    : IRequestHandler<ListOperationTemplateDefinitionsQuery, IReadOnlyList<TemplateDefinitionSummaryDto>>
{
    private readonly IOperationTemplateRepository _repo;
    public ListOperationTemplateDefinitionsHandler(IOperationTemplateRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TemplateDefinitionSummaryDto>> Handle(ListOperationTemplateDefinitionsQuery q, CancellationToken ct) =>
        (await _repo.ListAllAsync(ct))
            .Select(t => new TemplateDefinitionSummaryDto(t.Id, t.Code, t.Title, t.Description, t.IsActive,
                t.Parameters.Count, t.Lines.Count, t.CreatedBy, t.CreatedAtUtc))
            .ToList();
}

// ─────────── یک الگو با همهٔ جزئیات (برای ویرایش) ───────────

public sealed record GetOperationTemplateDefinitionQuery(Guid Id) : IRequest<TemplateDefinitionDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed record TemplateDefinitionDto(Guid Id, string Code, string Title, string Description,
    string VoucherDescriptionPattern, bool IsActive,
    IReadOnlyList<TemplateParameterDto> Parameters, IReadOnlyList<TemplateLineDefinitionDto> Lines, Guid? SystemTypeId, string? Keywords,
    IReadOnlyList<string>? AllowedVahedTypes = null);

public sealed record TemplateLineDefinitionDto(LineSide Side, Guid SubsidiaryAccountId, string? AccountCode, string? AccountTitle,
    string? AmountParameterKey, decimal Percent, bool IsBalancingLine, string? DescriptionPattern, int SortOrder,
    IReadOnlyList<TemplateLineDetailDefinitionDto> Details);

public sealed record TemplateLineDetailDefinitionDto(int Level, string? ParameterKey, Guid? FixedDetailId, string? FixedDetailTitle);

public sealed class GetOperationTemplateDefinitionHandler : IRequestHandler<GetOperationTemplateDefinitionQuery, TemplateDefinitionDto>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly ISubsidiaryAccountReader _accounts;
    private readonly IDetailReader _details;

    public GetOperationTemplateDefinitionHandler(IOperationTemplateRepository repo, ISubsidiaryAccountReader accounts, IDetailReader details)
    { _repo = repo; _accounts = accounts; _details = details; }

    public async Task<TemplateDefinitionDto> Handle(GetOperationTemplateDefinitionQuery q, CancellationToken ct)
    {
        var t = await _repo.GetFullAsync(q.Id, ct) ?? throw new NotFoundException("OperationTemplate", q.Id);

        var accounts = await _accounts.GetByIdsAsync(t.Lines.Select(l => l.SubsidiaryAccountId).Distinct().ToList(), ct);
        var fixedIds = t.Lines.SelectMany(l => l.Details).Where(d => d.FixedDetailId.HasValue).Select(d => d.FixedDetailId!.Value).Distinct().ToList();
        var details = await _details.GetByIdsAsync(fixedIds, q.VahedCode, ct);

        return new TemplateDefinitionDto(t.Id, t.Code, t.Title, t.Description, t.VoucherDescriptionPattern, t.IsActive,
            t.Parameters.OrderBy(p => p.SortOrder)
                .Select(p => new TemplateParameterDto(p.Key, p.Title, p.Type, p.DetailGroupId, p.IsRequired, p.AskPrompt, p.SortOrder))
                .ToList(),
            t.Lines.OrderBy(l => l.SortOrder).Select(l =>
            {
                accounts.TryGetValue(l.SubsidiaryAccountId, out var acc);
                return new TemplateLineDefinitionDto(l.Side, l.SubsidiaryAccountId, acc?.Code, acc?.Title,
                    string.IsNullOrEmpty(l.AmountParameterKey) ? null : l.AmountParameterKey, l.Percent, l.IsBalancingLine,
                    l.DescriptionPattern, l.SortOrder,
                    l.Details.OrderBy(d => d.Level).Select(d => new TemplateLineDetailDefinitionDto(d.Level, d.ParameterKey, d.FixedDetailId,
                        d.FixedDetailId is Guid fid && details.TryGetValue(fid, out var det) ? det.Title : null)).ToList());
            }).ToList(),
            t.SystemTypeId, t.Keywords, TemplateMapping.SplitVahedTypes(t.AllowedVahedTypes));
    }
}

// ─────────── بررسی بدون ذخیره (دکمهٔ «بررسی الگو») ───────────

public sealed record ValidateOperationTemplateQuery(
    Guid? Id, string Code, string Title, string Description, string VoucherDescriptionPattern,
    List<TemplateParameterDto> Parameters, List<TemplateLineDto> Lines, Guid? SystemTypeId = null, string? Keywords = null,
    IReadOnlyList<string>? AllowedVahedTypes = null) : IRequest<CreateTemplateResult>;

public sealed class ValidateOperationTemplateHandler : IRequestHandler<ValidateOperationTemplateQuery, CreateTemplateResult>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly TemplateDefinitionValidator _validator;

    public ValidateOperationTemplateHandler(IOperationTemplateRepository repo, TemplateDefinitionValidator validator)
    { _repo = repo; _validator = validator; }

    public async Task<CreateTemplateResult> Handle(ValidateOperationTemplateQuery q, CancellationToken ct)
    {
        var t = TemplateMapping.Build(q.Id ?? Guid.Empty, q.Code, q.Title, q.Description, q.VoucherDescriptionPattern, q.Parameters, q.Lines, q.SystemTypeId, q.Keywords, q.AllowedVahedTypes);
        var errors = (await _validator.ValidateAsync(t, ct)).ToList();
        if (errors.Count == 0 && await _repo.CodeExistsForOtherAsync(t.Code, q.Id, ct))
            errors.Add($"کد الگو «{t.Code}» قبلاً ثبت شده است.");
        return new(errors.Count == 0, q.Id, errors);
    }
}

// ─────────── ویرایش (جایگزینی کامل تعریف) ───────────

public sealed record UpdateOperationTemplateCommand(
    string Code, string Title, string Description, string VoucherDescriptionPattern,
    List<TemplateParameterDto> Parameters, List<TemplateLineDto> Lines, Guid? SystemTypeId = null, string? Keywords = null,
    IReadOnlyList<string>? AllowedVahedTypes = null) : IRequest<CreateTemplateResult>
{
    /// <summary>از مسیر (<c>{id}/update</c>)، نه بدنه.</summary>
    [JsonIgnore]
    public Guid Id { get; set; }
}

public sealed class UpdateOperationTemplateHandler : IRequestHandler<UpdateOperationTemplateCommand, CreateTemplateResult>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly TemplateDefinitionValidator _validator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateOperationTemplateHandler(IOperationTemplateRepository repo, TemplateDefinitionValidator validator,
        IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _validator = validator; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<CreateTemplateResult> Handle(UpdateOperationTemplateCommand c, CancellationToken ct)
    {
        TemplateMapping.EnsureSetad(_currentUser);

        var candidate = TemplateMapping.Build(c.Id, c.Code, c.Title, c.Description, c.VoucherDescriptionPattern, c.Parameters, c.Lines, c.SystemTypeId, c.Keywords, c.AllowedVahedTypes);
        var errors = (await _validator.ValidateAsync(candidate, ct)).ToList();
        if (errors.Count == 0 && await _repo.CodeExistsForOtherAsync(candidate.Code, c.Id, ct))
            errors.Add($"کد الگو «{candidate.Code}» قبلاً ثبت شده است.");
        if (errors.Count > 0) return new(false, c.Id, errors);

        var t = await _repo.GetForUpdateAsync(c.Id, ct) ?? throw new NotFoundException("OperationTemplate", c.Id);

        // ردیف‌ها و پارامترها جایگزین می‌شوند؛ ردپای اجراهای قبلی (TB_OP_EXECUTION) فقط به خود الگو اشاره دارد.
        _repo.RemoveChildren(t);
        t.Code = candidate.Code;
        t.Title = candidate.Title;
        t.Description = candidate.Description;
        t.VoucherDescriptionPattern = candidate.VoucherDescriptionPattern;
        t.SystemTypeId = candidate.SystemTypeId;
        t.Keywords = candidate.Keywords;
        t.AllowedVahedTypes = candidate.AllowedVahedTypes;
        t.Parameters = candidate.Parameters;
        t.Lines = candidate.Lines;
        _repo.AddChildren(t);

        await _unitOfWork.SaveChangesAsync(ct);
        return new(true, t.Id, Array.Empty<string>());
    }
}

// ─────────── فعال/غیرفعال (به‌جای حذف) ───────────

public sealed record SetOperationTemplateActiveCommand(bool IsActive) : IRequest<Unit>
{
    [JsonIgnore]
    public Guid Id { get; set; }
}

public sealed class SetOperationTemplateActiveHandler : IRequestHandler<SetOperationTemplateActiveCommand, Unit>
{
    private readonly IOperationTemplateRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public SetOperationTemplateActiveHandler(IOperationTemplateRepository repo, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    { _repo = repo; _unitOfWork = unitOfWork; _currentUser = currentUser; }

    public async Task<Unit> Handle(SetOperationTemplateActiveCommand c, CancellationToken ct)
    {
        TemplateMapping.EnsureSetad(_currentUser);
        var t = await _repo.GetForUpdateAsync(c.Id, ct) ?? throw new NotFoundException("OperationTemplate", c.Id);
        t.IsActive = c.IsActive;
        await _unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
