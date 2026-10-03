using System.Globalization;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Engine;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.Preview;

/// <summary>
/// نتیجهٔ پیش‌نمایش یک نسخهٔ قالب: ردیف‌ها با مبلغ دورهٔ جاری (علامت حسابداری)، یا خطای محاسبه.
/// </summary>
public sealed record FsTemplatePreviewDto(
    string TemplateCode,
    string TitleFa,
    string? Error,
    IReadOnlyList<FsRunRowDto> Rows);

/// <summary>
/// <c>GET api/fs/template-versions/{id}/preview?year=&amp;toMonth=&amp;minDocLife=&amp;includeSubUnits=</c> — بخش
/// ۴۵-و «پیش‌نمایش زنده»: همین نسخه (حتی پیش‌نویس) روی اسناد واقعی واحد هدر محاسبه می‌شود، <b>بدون ذخیرهٔ
/// هیچ اجرایی</b>. برای <c>STMT</c>، بقیهٔ صورت‌های همان مجموعه همان‌طور انتخاب می‌شوند که اجرا با «استفاده
/// از پیش‌نویس‌ها» برمی‌دارد، و این نسخه جای قالب هم‌کدش می‌نشیند. فقط ستون جاری؛ مقادیر دستی صفر.
/// </summary>
public sealed record PreviewFsTemplateVersionQuery(Guid Id, string Year, int ToMonth, int MinDocLife, bool IncludeSubUnits)
    : IRequest<FsTemplatePreviewDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class PreviewFsTemplateVersionQueryValidator : AbstractValidator<PreviewFsTemplateVersionQuery>
{
    public PreviewFsTemplateVersionQueryValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Year).NotEmpty().Matches("^1[34][0-9]{2}$").WithMessage("سال مالی باید سال شمسی چهاررقمی باشد.");
        RuleFor(x => x.ToMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.MinDocLife).InclusiveBetween(1, 4);
    }
}

public sealed class PreviewFsTemplateVersionQueryHandler : IRequestHandler<PreviewFsTemplateVersionQuery, FsTemplatePreviewDto?>
{
    private readonly IFsTemplateReadRepository _templateReadRepository;
    private readonly IFsBalanceReadRepository _balanceReadRepository;
    private readonly IFsUnitScopeProvider _scopes;

    public PreviewFsTemplateVersionQueryHandler(
        IFsTemplateReadRepository templateReadRepository,
        IFsBalanceReadRepository balanceReadRepository,
        IFsUnitScopeProvider scopes)
    {
        _templateReadRepository = templateReadRepository;
        _balanceReadRepository = balanceReadRepository;
        _scopes = scopes;
    }

    public async Task<FsTemplatePreviewDto?> Handle(PreviewFsTemplateVersionQuery request, CancellationToken cancellationToken)
    {
        var scope = await _scopes.GetAsync(request.VahedCode, cancellationToken);
        var version = await _templateReadRepository.GetVersionAsync(request.Id, scope, cancellationToken);

        if (version is null)
        {
            return null;
        }

        var year = int.Parse(request.Year, CultureInfo.InvariantCulture);

        // بقیهٔ مجموعه برای STMT؛ این نسخه جای قالب هم‌کدش.
        var versions = (await _templateReadRepository.GetVersionsForRunAsync(version.Framework, year, true, scope, cancellationToken))
            .Where(v => v.TemplateCode != version.TemplateCode)
            .Append(version)
            .ToList();

        var units = request.IncludeSubUnits ? scope.Accessible.ToList() : new List<string> { request.VahedCode };
        var raw = await _balanceReadRepository.GetBalancesAsync(
            request.Year, $"{year:0000}0101", $"{year:0000}{request.ToMonth:00}31", units, request.MinDocLife, cancellationToken);

        var totals = raw
            .GroupBy(b => b.AccCode, StringComparer.Ordinal)
            .Select(g => new FsAccountBalance(
                g.Key, g.First().AccName,
                g.Sum(b => b.OpeningDebtor), g.Sum(b => b.OpeningCreditor), g.Sum(b => b.PeriodDebtor), g.Sum(b => b.PeriodCreditor)))
            .ToList();

        IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>>? values = null;
        string? error = null;

        try
        {
            values = FsStatementEngine.Compute(
                versions.Select(v => new FsEngineStatement(
                    v.TemplateCode,
                    v.Rows.Select(r => new FsEngineRow(r.Code, r.OrderNo, r.RowType, r.Selector, r.ValueType, r.Formula)).ToList()))
                    .ToList(),
                new Dictionary<string, IReadOnlyList<FsAccountBalance>>(StringComparer.Ordinal) { [FsColumns.Current] = totals });
        }
        catch (Exception ex) when (ex is FsEngineException or FsExpressionException or KeyNotFoundException)
        {
            error = "محاسبه ممکن نشد: " + ex.Message;
        }

        return new FsTemplatePreviewDto(
            version.TemplateCode,
            version.TemplateTitleFa,
            error,
            version.Rows.Select(r => new FsRunRowDto(
                r.Id,
                r.Code,
                r.ParentCode,
                r.OrderNo,
                r.RowType,
                r.TitleFa,
                r.TitleEn,
                r.NoteRef,
                r.NormalBalance,
                r.Selector,
                r.ValueType,
                r.Formula,
                r.Format,
                r.IsDrillable,
                values?.GetValueOrDefault((version.TemplateCode, r.Code))?.GetValueOrDefault(FsColumns.Current)?.Amount is { } a
                    ? decimal.Round(a)
                    : null,
                null)).ToList());
    }
}
