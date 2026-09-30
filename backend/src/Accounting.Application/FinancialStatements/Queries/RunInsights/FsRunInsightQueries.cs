using System.Globalization;
using System.Text.Json.Serialization;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.RunInsights;

/// <summary>
/// <c>GET api/fs/runs/{id}/staleness</c> — بخش ۴۵-ه: مانده‌های منبع را با همان پارامترهای اجرا دوباره
/// می‌خواند و اثر انگشتش را با <c>BALANCE_HASH</c> مقایسه می‌کند. تفاوت = اسناد پس از اجرا تغییر کرده‌اند.
/// </summary>
public sealed record GetFsRunStalenessQuery(Guid Id) : IRequest<FsRunStalenessDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary><c>GET api/fs/runs/{a}/diff/{b}</c> — مقایسهٔ ردیف‌به‌ردیف دو اجرای همین واحد (ستون جاری).</summary>
public sealed record GetFsRunDiffQuery(Guid RunA, Guid RunB) : IRequest<IReadOnlyList<FsRunDiffRowDto>?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetFsRunStalenessQueryHandler : IRequestHandler<GetFsRunStalenessQuery, FsRunStalenessDto?>
{
    private readonly IFsRunRepository _runRepository;
    private readonly IFsBalanceReadRepository _balanceReadRepository;
    private readonly IFsUnitScopeProvider _scopes;

    public GetFsRunStalenessQueryHandler(IFsRunRepository runRepository, IFsBalanceReadRepository balanceReadRepository, IFsUnitScopeProvider scopes)
    {
        _runRepository = runRepository;
        _balanceReadRepository = balanceReadRepository;
        _scopes = scopes;
    }

    public async Task<FsRunStalenessDto?> Handle(GetFsRunStalenessQuery request, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetForUpdateAsync(request.Id, request.VahedCode, cancellationToken);

        if (run is null)
        {
            return null;
        }

        if (run.BALANCE_HASH is null)
        {
            return new FsRunStalenessDto(false, true);
        }

        // دامنهٔ واحدهای امروز؛ اگر درخت واحدها عوض شده باشد، این هم «کهنه» حساب می‌شود — درست است.
        var units = run.INCLUDE_SUBUNITS
            ? (await _scopes.GetAsync(run.VAHEDCODE, cancellationToken)).Accessible.ToList()
            : new List<string> { run.VAHEDCODE };

        var raw = new Dictionary<string, IReadOnlyList<FsAccountBalance>>(StringComparer.Ordinal)
        {
            [FsColumns.Current] = await _balanceReadRepository.GetBalancesAsync(
                run.YEAR, run.FROM_DATE, run.TO_DATE, units, run.MIN_DOCLIFE, cancellationToken),
        };

        if (run.HAS_PRIOR)
        {
            var prior = (int.Parse(run.YEAR, CultureInfo.InvariantCulture) - 1).ToString("0000", CultureInfo.InvariantCulture);
            raw[FsColumns.Prior] = await _balanceReadRepository.GetBalancesAsync(
                prior, prior + run.FROM_DATE[4..], prior + run.TO_DATE[4..], units, run.MIN_DOCLIFE, cancellationToken);
        }

        return new FsRunStalenessDto(FsBalanceHash.Compute(raw) != run.BALANCE_HASH, false);
    }
}

public sealed class GetFsRunDiffQueryHandler : IRequestHandler<GetFsRunDiffQuery, IReadOnlyList<FsRunDiffRowDto>?>
{
    private readonly IFsRunRepository _runRepository;

    public GetFsRunDiffQueryHandler(IFsRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public async Task<IReadOnlyList<FsRunDiffRowDto>?> Handle(GetFsRunDiffQuery request, CancellationToken cancellationToken)
    {
        var a = await _runRepository.GetDetailAsync(request.RunA, request.VahedCode, cancellationToken);
        var b = await _runRepository.GetDetailAsync(request.RunB, request.VahedCode, cancellationToken);

        if (a is null || b is null)
        {
            return null;
        }

        static bool IsValue(FsRowType t) => t is FsRowType.Account or FsRowType.Formula or FsRowType.External;

        var bRows = b.Statements
            .SelectMany(s => s.Rows.Where(r => IsValue(r.RowType)).Select(r => (s.TemplateCode, r.Code, r.AmountCur)))
            .ToDictionary(x => (x.TemplateCode, x.Code), x => x.AmountCur);

        var result = new List<FsRunDiffRowDto>();
        var seen = new HashSet<(string, string)>();

        foreach (var s in a.Statements)
        {
            foreach (var r in s.Rows.Where(r => IsValue(r.RowType)))
            {
                seen.Add((s.TemplateCode, r.Code));
                result.Add(new FsRunDiffRowDto(
                    s.TemplateCode, s.TitleFa, s.IsNote, r.Code, r.TitleFa, r.NormalBalance,
                    r.AmountCur, bRows.TryGetValue((s.TemplateCode, r.Code), out var bv) ? bv : null));
            }
        }

        // ردیف‌هایی که فقط در اجرای دوم هستند (قالب عوض شده).
        foreach (var s in b.Statements)
        {
            foreach (var r in s.Rows.Where(r => IsValue(r.RowType) && !seen.Contains((s.TemplateCode, r.Code))))
            {
                result.Add(new FsRunDiffRowDto(s.TemplateCode, s.TitleFa, s.IsNote, r.Code, r.TitleFa, r.NormalBalance, null, r.AmountCur));
            }
        }

        return result;
    }
}
