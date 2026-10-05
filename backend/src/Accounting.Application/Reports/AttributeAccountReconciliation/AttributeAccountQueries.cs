using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using FluentValidation;
using MediatR;
using System.Text.Json.Serialization;

namespace Accounting.Application.Reports.AttributeAccountReconciliation;

/// <summary>معیارهای مشترک سه سطح «مغایرت‌گیری حساب‌های شناسه‌دار» (FINACC-523).</summary>
public interface IAttributeAccountCriteria
{
    string Year { get; }
    string? FromDate { get; }
    string? ToDate { get; }
    int? DocLife { get; }
}

/// <summary>سطح ۱ — معین‌های شناسه‌دار واحد جاری.</summary>
public sealed record GetAttributeAccountMoeinsQuery(
    string Year,
    string? FromDate = null,
    string? ToDate = null,
    int? DocLife = null) : IRequest<IReadOnlyList<AttributeAccountMoeinDto>>, IVahedScopedQuery, IAttributeAccountCriteria
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>سطح ۲ — شناسه‌های یک معین.</summary>
public sealed record GetAttributeAccountValuesQuery(
    string Year,
    Guid AccountId,
    string? FromDate = null,
    string? ToDate = null,
    int? DocLife = null) : IRequest<IReadOnlyList<AttributeAccountValueDto>>, IVahedScopedQuery, IAttributeAccountCriteria
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>
/// سطح ۳ — ردیف‌های سند یک شناسه. <paramref name="WithoutIdentifier"/> = ردیف‌های بی‌شناسهٔ معین
/// (آنگاه <paramref name="AttributeValue"/> نادیده گرفته می‌شود).
/// </summary>
public sealed record GetAttributeAccountLinesQuery(
    string Year,
    Guid AccountId,
    string? AttributeValue,
    bool WithoutIdentifier = false,
    string? FromDate = null,
    string? ToDate = null,
    int? DocLife = null) : IRequest<IReadOnlyList<AttributeAccountLineDto>>, IVahedScopedQuery, IAttributeAccountCriteria
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetAttributeAccountQueriesHandler :
    IRequestHandler<GetAttributeAccountMoeinsQuery, IReadOnlyList<AttributeAccountMoeinDto>>,
    IRequestHandler<GetAttributeAccountValuesQuery, IReadOnlyList<AttributeAccountValueDto>>,
    IRequestHandler<GetAttributeAccountLinesQuery, IReadOnlyList<AttributeAccountLineDto>>
{
    private readonly IAttributeAccountReconciliationReadRepository _repository;

    public GetAttributeAccountQueriesHandler(IAttributeAccountReconciliationReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<AttributeAccountMoeinDto>> Handle(
        GetAttributeAccountMoeinsQuery request, CancellationToken cancellationToken)
        => AttributeReconciliationCalculator.Moeins(
            await _repository.GetLinesAsync(Filter(request, request.VahedCode, null), cancellationToken));

    public async Task<IReadOnlyList<AttributeAccountValueDto>> Handle(
        GetAttributeAccountValuesQuery request, CancellationToken cancellationToken)
        => AttributeReconciliationCalculator.Values(
            await _repository.GetLinesAsync(Filter(request, request.VahedCode, request.AccountId), cancellationToken));

    public async Task<IReadOnlyList<AttributeAccountLineDto>> Handle(
        GetAttributeAccountLinesQuery request, CancellationToken cancellationToken)
    {
        var lines = await _repository.GetLinesAsync(Filter(request, request.VahedCode, request.AccountId), cancellationToken);
        var wanted = request.WithoutIdentifier ? null : AttributeReconciliationCalculator.NormalizeValue(request.AttributeValue);
        return lines
            .Where(l => AttributeReconciliationCalculator.NormalizeValue(l.AttributeValue) == wanted)
            .OrderBy(l => l.DateDoc, StringComparer.Ordinal)
            .ThenBy(l => l.DocNum, StringComparer.Ordinal)
            .Select(l => new AttributeAccountLineDto(
                l.LineId, l.VoucherHeadId, l.DocNum, l.DateDoc, l.DocLife, l.HeadDesc, l.LineDesc, l.Debtor, l.Creditor))
            .ToList();
    }

    private static AttributeAccountFilter Filter(IAttributeAccountCriteria c, string vahedCode, Guid? accountId)
        => new(vahedCode, c.Year, Blank(c.FromDate), Blank(c.ToDate), c.DocLife, accountId);

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

internal static class AttributeAccountCriteriaRules
{
    public static void Apply<T>(AbstractValidator<T> v) where T : IAttributeAccountCriteria
    {
        v.RuleFor(x => x.Year)
            .NotEmpty().WithMessage("سال مالی الزامی است.")
            .Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم عددی باشد.");
        v.RuleFor(x => x.FromDate)
            .Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate));
        v.RuleFor(x => x.ToDate)
            .Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.ToDate));
        v.RuleFor(x => x.DocLife)
            .InclusiveBetween(1, 4).WithMessage("وضعیت سند نامعتبر است.")
            .When(x => x.DocLife.HasValue);
        v.RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .WithMessage("«از تاریخ» نباید بعد از «تا تاریخ» باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate) && !string.IsNullOrWhiteSpace(x.ToDate));
    }
}

public sealed class GetAttributeAccountMoeinsQueryValidator : AbstractValidator<GetAttributeAccountMoeinsQuery>
{
    public GetAttributeAccountMoeinsQueryValidator() => AttributeAccountCriteriaRules.Apply(this);
}

public sealed class GetAttributeAccountValuesQueryValidator : AbstractValidator<GetAttributeAccountValuesQuery>
{
    public GetAttributeAccountValuesQueryValidator()
    {
        AttributeAccountCriteriaRules.Apply(this);
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("معین الزامی است.");
    }
}

public sealed class GetAttributeAccountLinesQueryValidator : AbstractValidator<GetAttributeAccountLinesQuery>
{
    public GetAttributeAccountLinesQueryValidator()
    {
        AttributeAccountCriteriaRules.Apply(this);
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("معین الزامی است.");
        RuleFor(x => x.AttributeValue)
            .NotEmpty().WithMessage("مقدار شناسه الزامی است.")
            .When(x => !x.WithoutIdentifier);
        RuleFor(x => x.AttributeValue).MaximumLength(100);
    }
}
