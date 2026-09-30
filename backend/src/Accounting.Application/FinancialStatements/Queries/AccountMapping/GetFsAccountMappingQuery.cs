using System.Text.Json.Serialization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.AccountMapping;

/// <summary>یک معین کدینگ برای نمای نگاشت.</summary>
public sealed record FsChartMoein(string AccCode, string? AccName, string? KolCode, string? KolName, string? GroupCode, string? GroupName);

/// <summary>ردیفی که یک معین را انتخاب می‌کند.</summary>
public sealed record FsMappingMatchDto(
    string TemplateCode,
    string TemplateTitle,
    bool IsNote,
    string RowCode,
    string? RowTitle,
    string? Side);

/// <summary>
/// یک معین و ردیف‌هایی که انتخابش می‌کنند. <paramref name="StatementMatchCount"/> فقط صورت‌های اصلی (نه
/// یادداشت‌ها) — صفر = «بدون نگاشت»؛ <paramref name="DoubleCounted"/> = در یک صورت در دو ردیف بدون [D]/[C].
/// </summary>
public sealed record FsAccountMappingDto(
    string AccCode,
    string? AccName,
    string? KolCode,
    string? KolName,
    string? GroupCode,
    string? GroupName,
    int StatementMatchCount,
    bool DoubleCounted,
    IReadOnlyList<FsMappingMatchDto> Matches);

/// <summary>
/// <c>GET api/fs/account-mapping?framework=&amp;year=&amp;useDrafts=</c> — بخش ۴۵-و، «نگاشت حساب‌ها» (سند منبع
/// §۱۲-۳): نمای حساب‌محورِ همان قالب‌هایی که اجرای این مجموعه برای واحد هدر برمی‌دارد (قالب اختصاصی نزدیک‌ترین
/// مالک، وگرنه مشترک؛ نسخهٔ فعال سال، یا پیش‌نویس اگر <paramref name="UseDrafts"/>). همهٔ معین‌های حذف‌نشدهٔ
/// کدینگ، نه فقط دارای مانده — مکمل کنترل V-05.
/// </summary>
public sealed record GetFsAccountMappingQuery(FsFramework Framework, int Year, bool UseDrafts)
    : IRequest<IReadOnlyList<FsAccountMappingDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetFsAccountMappingQueryValidator : AbstractValidator<GetFsAccountMappingQuery>
{
    public GetFsAccountMappingQueryValidator()
    {
        RuleFor(x => x.Framework).IsInEnum();
        RuleFor(x => x.Year).InclusiveBetween(1300, 1600);
    }
}

public sealed class GetFsAccountMappingQueryHandler : IRequestHandler<GetFsAccountMappingQuery, IReadOnlyList<FsAccountMappingDto>>
{
    private readonly IFsTemplateReadRepository _templateReadRepository;
    private readonly IFsBalanceReadRepository _balanceReadRepository;
    private readonly IFsUnitScopeProvider _scopes;

    public GetFsAccountMappingQueryHandler(
        IFsTemplateReadRepository templateReadRepository,
        IFsBalanceReadRepository balanceReadRepository,
        IFsUnitScopeProvider scopes)
    {
        _templateReadRepository = templateReadRepository;
        _balanceReadRepository = balanceReadRepository;
        _scopes = scopes;
    }

    public async Task<IReadOnlyList<FsAccountMappingDto>> Handle(GetFsAccountMappingQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        var versions = await _templateReadRepository.GetVersionsForRunAsync(request.Framework, request.Year, request.UseDrafts, scope, cancellationToken);
        var moeins = await _balanceReadRepository.GetChartMoeinsAsync(cancellationToken);

        var selectors = versions
            .SelectMany(v => v.Rows
                .Where(r => r.RowType == FsRowType.Account && r.Selector is not null)
                .Select(r => (Version: v, Row: r, Selector: AccountSelector.TryParse(r.Selector, out var s, out _) ? s : null)))
            .Where(x => x.Selector is not null)
            .ToList();

        return moeins
            .Select(m =>
            {
                var matches = selectors
                    .Select(x => (x.Version, x.Row, Term: x.Selector!.Match(m.AccCode)))
                    .Where(x => x.Term is not null)
                    .ToList();

                var isNote = (FsTemplateVersionDetailDto v) => v.StatementType == FsStatementType.Note;
                var main = matches.Where(x => !isNote(x.Version)).ToList();
                var doubled = main
                    .Where(x => x.Term!.Side == SelectorBalanceSide.Any)
                    .GroupBy(x => x.Version.TemplateCode)
                    .Any(g => g.Count() > 1);

                return new FsAccountMappingDto(
                    m.AccCode, m.AccName, m.KolCode, m.KolName, m.GroupCode, m.GroupName,
                    main.Count,
                    doubled,
                    matches.Select(x => new FsMappingMatchDto(
                        x.Version.TemplateCode,
                        x.Version.TemplateTitleFa,
                        isNote(x.Version),
                        x.Row.Code,
                        x.Row.TitleFa,
                        x.Term!.Side switch
                        {
                            SelectorBalanceSide.DebitOnly => "D",
                            SelectorBalanceSide.CreditOnly => "C",
                            _ => null,
                        })).ToList());
            })
            .ToList();
    }
}
