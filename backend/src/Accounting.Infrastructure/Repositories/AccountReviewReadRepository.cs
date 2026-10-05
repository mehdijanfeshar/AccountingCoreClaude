using Accounting.Application.Common.Security;
using System.Linq.Expressions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.AccountReview;
using Accounting.Application.Reports.AccountReview.GetAccountReview;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAccountReviewReadRepository"/>, reading the
/// <c>VW_CONSOLIDATE_REPORT</c> view.
///
/// <para>
/// <b>LINQ, not raw SQL — unlike the reference project and unlike our own trial balance.</b> The
/// reference builds this query by string concatenation, switching the grouped column name into the
/// SQL text per level, which is also what forces it to hand-build its WHERE clause and its Oracle
/// parameters. Here the level only decides which <see cref="Expression"/> the rows are projected
/// through, so every filter stays a normal parameterised LINQ predicate and no user-supplied value
/// ever reaches a SQL string.
/// </para>
///
/// <para>
/// ⚠️ The expressions below must stay expressions EF can translate. Calling <c>.Compile()</c> on
/// one — the obvious-looking way to pick a column per level — would move the grouping into memory
/// and pull the entire view across the wire before aggregating.
/// </para>
///
/// <para>
/// ⚠️ <b>No boolean ever reaches the generated SQL.</b> Everything that is conceptually a yes/no —
/// "does this row have children", "does this level carry data" — is counted, not tested, and
/// converted to a bool in C# afterwards. Oracle has no boolean literal, and this project has been
/// bitten by that twice (phases 37 and 40).
/// </para>
/// </summary>
public sealed class AccountReviewReadRepository : IAccountReviewReadRepository
{
    private readonly LegacyDbContext _dbContext;

    private readonly IReportUnitScope? _unitScope;

    public AccountReviewReadRepository(LegacyDbContext dbContext, IReportUnitScope? unitScope = null)
    {
        _dbContext = dbContext;
        _unitScope = unitScope;
    }

    /// <summary>
    /// Level-independent shape the view rows are normalised into before grouping. Making the level
    /// a projection rather than a grouping key is what keeps the whole query translatable.
    ///
    /// <para>
    /// <see cref="NextCode"/> carries the code one level down, which is how a row learns whether
    /// it can be drilled into without a second query per row.
    /// </para>
    /// </summary>
    private sealed class LevelRow
    {
        public string? Code { get; set; }

        public string? Name { get; set; }

        public string? NextCode { get; set; }

        public decimal? Debtor { get; set; }

        public decimal? Creditor { get; set; }
    }

    public async Task<AccountReviewResultDto> GetAsync(
        GetAccountReviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var rows = Filter(query);

        var aggregates = await rows
            .Select(LevelProjection(query.Level))
            // A line carrying no code at the selected level is excluded, not grouped under an
            // empty key: a تفصیلی level a line was never assigned at is absence, not a category.
            // The reference applies this only to تفصیلی levels; doing it uniformly also stops a
            // malformed coding row from producing a blank-titled group.
            .Where(x => x.Code != null && x.Code != "")
            .GroupBy(x => new { x.Code, x.Name })
            .Select(g => new
            {
                g.Key.Code,
                g.Key.Name,
                Debtor = g.Sum(x => x.Debtor ?? 0m),
                Creditor = g.Sum(x => x.Creditor ?? 0m),
                // Counted, never tested — see the class remarks on boolean literals.
                ChildCount = g.Count(x => x.NextCode != null && x.NextCode != ""),
            })
            .ToListAsync(cancellationToken);

        var label = LevelLabel(query.Level);
        var scope = query.Scope ?? Array.Empty<AccountReviewScopeItem>();

        var reportRows = aggregates
            .OrderBy(a => a.Code, StringComparer.Ordinal)
            .Select(a => new AccountReviewRowDto(
                a.Code ?? string.Empty,
                a.Name ?? string.Empty,
                label,
                a.Debtor,
                a.Creditor,
                // One-sided balances, matching the reference's GREATEST(...): exactly one of the
                // two is non-zero for any row, which is what lets the report read as two columns
                // rather than one signed number. Computed after materialisation — the grouped set
                // is one row per account, and GREATEST/CASE translation is a needless risk here.
                DebtorBalance: Math.Max(a.Debtor - a.Creditor, 0m),
                CreditorBalance: Math.Max(a.Creditor - a.Debtor, 0m),
                HasChildren: a.ChildCount > 0))
            .ToList();

        return new AccountReviewResultDto(
            reportRows,
            query.Level,
            label,
            await ResolveScopeAsync(rows, scope, cancellationToken),
            await AvailableLevelsAsync(rows, cancellationToken));
    }

    /// <summary>
    /// Echoes the caller's path back with each step's name resolved, for the breadcrumb.
    ///
    /// <para>
    /// One round trip: the scope has already narrowed the set to lines that match every step, so
    /// any single line carries the right name for all of them. A step whose code matches nothing
    /// still appears, with an empty name — dropping it would make a mistyped code look like it was
    /// never requested.
    /// </para>
    /// </summary>
    private static async Task<IReadOnlyList<AccountReviewScopeDto>> ResolveScopeAsync(
        IQueryable<VW_CONSOLIDATE_REPORT> rows,
        IReadOnlyList<AccountReviewScopeItem> scope,
        CancellationToken cancellationToken)
    {
        if (scope.Count == 0)
        {
            return Array.Empty<AccountReviewScopeDto>();
        }

        var names = await rows
            .Select(v => new
            {
                v.GROUPNAME,
                v.KOLNAME,
                v.MOINNAME,
                v.TAFSILINAME1,
                v.TAFSILINAME2,
                v.TAFSILINAME3,
                v.TAFSILINAME4,
                v.TAFSILINAME5,
                v.TAFSILINAME6,
                v.TAFSILINAME7,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return scope
            .OrderBy(s => s.Level)
            .Select(s => new AccountReviewScopeDto(
                s.Level,
                LevelLabel(s.Level),
                s.Code,
                (names is null ? null : s.Level switch
                {
                    AccountReviewLevel.Group => names.GROUPNAME,
                    AccountReviewLevel.Kol => names.KOLNAME,
                    AccountReviewLevel.Moin => names.MOINNAME,
                    AccountReviewLevel.Tafsili1 => names.TAFSILINAME1,
                    AccountReviewLevel.Tafsili2 => names.TAFSILINAME2,
                    AccountReviewLevel.Tafsili3 => names.TAFSILINAME3,
                    AccountReviewLevel.Tafsili4 => names.TAFSILINAME4,
                    AccountReviewLevel.Tafsili5 => names.TAFSILINAME5,
                    AccountReviewLevel.Tafsili6 => names.TAFSILINAME6,
                    AccountReviewLevel.Tafsili7 => names.TAFSILINAME7,
                    _ => null,
                }) ?? string.Empty))
            .ToList();
    }

    /// <summary>
    /// Which levels carry any code inside the current scope — the answer to «کدام سطوح؟».
    ///
    /// <para>
    /// Ten counts in a single aggregate round trip, rather than ten <c>Any()</c> calls: counts
    /// translate to <c>COUNT(CASE WHEN … THEN 1 END)</c>, whereas an <c>Any()</c> in a projection
    /// invites exactly the boolean-literal translation Oracle rejects.
    /// </para>
    /// </summary>
    private static async Task<IReadOnlyList<AccountReviewLevel>> AvailableLevelsAsync(
        IQueryable<VW_CONSOLIDATE_REPORT> rows,
        CancellationToken cancellationToken)
    {
        var counts = await rows
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Group = g.Count(v => v.GROUPCODE != null && v.GROUPCODE != ""),
                Kol = g.Count(v => v.KOLCODE != null && v.KOLCODE != ""),
                Moin = g.Count(v => v.MOINCODE != null && v.MOINCODE != ""),
                T1 = g.Count(v => v.TAFSILICODE1 != null && v.TAFSILICODE1 != ""),
                T2 = g.Count(v => v.TAFSILICODE2 != null && v.TAFSILICODE2 != ""),
                T3 = g.Count(v => v.TAFSILICODE3 != null && v.TAFSILICODE3 != ""),
                T4 = g.Count(v => v.TAFSILICODE4 != null && v.TAFSILICODE4 != ""),
                T5 = g.Count(v => v.TAFSILICODE5 != null && v.TAFSILICODE5 != ""),
                T6 = g.Count(v => v.TAFSILICODE6 != null && v.TAFSILICODE6 != ""),
                T7 = g.Count(v => v.TAFSILICODE7 != null && v.TAFSILICODE7 != ""),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (counts is null)
        {
            return Array.Empty<AccountReviewLevel>();
        }

        var pairs = new (AccountReviewLevel Level, int Count)[]
        {
            (AccountReviewLevel.Group, counts.Group),
            (AccountReviewLevel.Kol, counts.Kol),
            (AccountReviewLevel.Moin, counts.Moin),
            (AccountReviewLevel.Tafsili1, counts.T1),
            (AccountReviewLevel.Tafsili2, counts.T2),
            (AccountReviewLevel.Tafsili3, counts.T3),
            (AccountReviewLevel.Tafsili4, counts.T4),
            (AccountReviewLevel.Tafsili5, counts.T5),
            (AccountReviewLevel.Tafsili6, counts.T6),
            (AccountReviewLevel.Tafsili7, counts.T7),
        };

        return pairs.Where(p => p.Count > 0).Select(p => p.Level).ToList();
    }

    private IQueryable<VW_CONSOLIDATE_REPORT> Filter(GetAccountReviewQuery query)
    {
        var rows = _dbContext.VW_CONSOLIDATE_REPORTs
            .AsNoTracking()
            // Compared as a number, never as a boolean — see VW_CONSOLIDATE_REPORT.ISDELETED.
            // `!= true` on a bool? renders `<> True`, which Oracle rejects with ORA-00904.
            .Where(v => v.ISDELETED == null || v.ISDELETED != 1)
            .Where(v => v.YEAR == query.Year)
            // دامنهٔ چندواحدی (همهٔ واحدها / زیرمجموعه / گروه) — ReportUnitScopeBehavior؛ وگرنه فقط واحد جاری.
            .WhereVahed(v => v.VAHEDCODE, query.VahedCode, _unitScope?.VahedCodes);

        // The drill-down path. Each step is one equality on the level's own column, which the view
        // has already flattened onto every line — so depth costs no joins.
        foreach (var step in query.Scope ?? Array.Empty<AccountReviewScopeItem>())
        {
            rows = rows.Where(ScopePredicate(step.Level, step.Code));
        }

        // VOUCHERDATE (YYYYMMDD) and VOUCHERNUMBER are fixed-width strings in this schema, so
        // ordinal comparison is chronological / numerical with no conversion needed.
        if (!string.IsNullOrWhiteSpace(query.FromDate))
        {
            rows = rows.Where(v => v.VOUCHERDATE != null && string.Compare(v.VOUCHERDATE, query.FromDate) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.ToDate))
        {
            rows = rows.Where(v => v.VOUCHERDATE != null && string.Compare(v.VOUCHERDATE, query.ToDate) <= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.FromVoucherNo))
        {
            rows = rows.Where(v => v.VOUCHERNUMBER != null
                && string.Compare(v.VOUCHERNUMBER, query.FromVoucherNo) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.ToVoucherNo))
        {
            rows = rows.Where(v => v.VOUCHERNUMBER != null
                && string.Compare(v.VOUCHERNUMBER, query.ToVoucherNo) <= 0);
        }

        if (query.DocLife.HasValue)
        {
            rows = rows.Where(v => v.DOCLIFE == query.DocLife.Value);
        }

        if (query.SystemTypeId.HasValue)
        {
            rows = rows.Where(v => v.SYSID == query.SystemTypeId.Value);
        }

        return rows;
    }

    /// <summary>Equality predicate on one level's code column.</summary>
    private static Expression<Func<VW_CONSOLIDATE_REPORT, bool>> ScopePredicate(
        AccountReviewLevel level,
        string code)
        => level switch
        {
            AccountReviewLevel.Group => v => v.GROUPCODE == code,
            AccountReviewLevel.Kol => v => v.KOLCODE == code,
            AccountReviewLevel.Moin => v => v.MOINCODE == code,
            AccountReviewLevel.Tafsili1 => v => v.TAFSILICODE1 == code,
            AccountReviewLevel.Tafsili2 => v => v.TAFSILICODE2 == code,
            AccountReviewLevel.Tafsili3 => v => v.TAFSILICODE3 == code,
            AccountReviewLevel.Tafsili4 => v => v.TAFSILICODE4 == code,
            AccountReviewLevel.Tafsili5 => v => v.TAFSILICODE5 == code,
            AccountReviewLevel.Tafsili6 => v => v.TAFSILICODE6 == code,
            AccountReviewLevel.Tafsili7 => v => v.TAFSILICODE7 == code,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown matrix report level."),
        };

    private static Expression<Func<VW_CONSOLIDATE_REPORT, LevelRow>> LevelProjection(AccountReviewLevel level)
        => level switch
        {
            AccountReviewLevel.Group => v => new LevelRow
            { Code = v.GROUPCODE, Name = v.GROUPNAME, NextCode = v.KOLCODE, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            AccountReviewLevel.Kol => v => new LevelRow
            { Code = v.KOLCODE, Name = v.KOLNAME, NextCode = v.MOINCODE, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            AccountReviewLevel.Moin => v => new LevelRow
            { Code = v.MOINCODE, Name = v.MOINNAME, NextCode = v.TAFSILICODE1, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            AccountReviewLevel.Tafsili1 => v => new LevelRow
            { Code = v.TAFSILICODE1, Name = v.TAFSILINAME1, NextCode = v.TAFSILICODE2, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            AccountReviewLevel.Tafsili2 => v => new LevelRow
            { Code = v.TAFSILICODE2, Name = v.TAFSILINAME2, NextCode = v.TAFSILICODE3, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            AccountReviewLevel.Tafsili3 => v => new LevelRow
            { Code = v.TAFSILICODE3, Name = v.TAFSILINAME3, NextCode = v.TAFSILICODE4, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            AccountReviewLevel.Tafsili4 => v => new LevelRow
            { Code = v.TAFSILICODE4, Name = v.TAFSILINAME4, NextCode = v.TAFSILICODE5, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            AccountReviewLevel.Tafsili5 => v => new LevelRow
            { Code = v.TAFSILICODE5, Name = v.TAFSILINAME5, NextCode = v.TAFSILICODE6, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            AccountReviewLevel.Tafsili6 => v => new LevelRow
            { Code = v.TAFSILICODE6, Name = v.TAFSILINAME6, NextCode = v.TAFSILICODE7, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            // Deepest level: nothing below it, so no row here can ever offer a drill-down.
            AccountReviewLevel.Tafsili7 => v => new LevelRow
            { Code = v.TAFSILICODE7, Name = v.TAFSILINAME7, NextCode = null, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown matrix report level."),
        };

    /// <summary>Persian label for the level, shown as the row's «سطح» in the report.</summary>
    private static string LevelLabel(AccountReviewLevel level) => level switch
    {
        AccountReviewLevel.Group => "گروه",
        AccountReviewLevel.Kol => "کل",
        AccountReviewLevel.Moin => "معین",
        AccountReviewLevel.Tafsili1 => "تفصیلی ۱",
        AccountReviewLevel.Tafsili2 => "تفصیلی ۲",
        AccountReviewLevel.Tafsili3 => "تفصیلی ۳",
        AccountReviewLevel.Tafsili4 => "تفصیلی ۴",
        AccountReviewLevel.Tafsili5 => "تفصیلی ۵",
        AccountReviewLevel.Tafsili6 => "تفصیلی ۶",
        AccountReviewLevel.Tafsili7 => "تفصیلی ۷",
        _ => "تفصیلی",
    };
}
