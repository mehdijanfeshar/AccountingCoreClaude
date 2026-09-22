using System.Linq.Expressions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.MatrixReport;
using Accounting.Application.Reports.MatrixReport.GetMatrixReport;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IMatrixReportReadRepository"/>, reading the
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
public sealed class MatrixReportReadRepository : IMatrixReportReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public MatrixReportReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
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

    public async Task<MatrixReportResultDto> GetAsync(
        GetMatrixReportQuery query,
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
        var scope = query.Scope ?? Array.Empty<MatrixReportScopeItem>();

        var reportRows = aggregates
            .OrderBy(a => a.Code, StringComparer.Ordinal)
            .Select(a => new MatrixReportRowDto(
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

        return new MatrixReportResultDto(
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
    private static async Task<IReadOnlyList<MatrixReportScopeDto>> ResolveScopeAsync(
        IQueryable<VW_CONSOLIDATE_REPORT> rows,
        IReadOnlyList<MatrixReportScopeItem> scope,
        CancellationToken cancellationToken)
    {
        if (scope.Count == 0)
        {
            return Array.Empty<MatrixReportScopeDto>();
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
            .Select(s => new MatrixReportScopeDto(
                s.Level,
                LevelLabel(s.Level),
                s.Code,
                (names is null ? null : s.Level switch
                {
                    MatrixReportLevel.Group => names.GROUPNAME,
                    MatrixReportLevel.Kol => names.KOLNAME,
                    MatrixReportLevel.Moin => names.MOINNAME,
                    MatrixReportLevel.Tafsili1 => names.TAFSILINAME1,
                    MatrixReportLevel.Tafsili2 => names.TAFSILINAME2,
                    MatrixReportLevel.Tafsili3 => names.TAFSILINAME3,
                    MatrixReportLevel.Tafsili4 => names.TAFSILINAME4,
                    MatrixReportLevel.Tafsili5 => names.TAFSILINAME5,
                    MatrixReportLevel.Tafsili6 => names.TAFSILINAME6,
                    MatrixReportLevel.Tafsili7 => names.TAFSILINAME7,
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
    private static async Task<IReadOnlyList<MatrixReportLevel>> AvailableLevelsAsync(
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
            return Array.Empty<MatrixReportLevel>();
        }

        var pairs = new (MatrixReportLevel Level, int Count)[]
        {
            (MatrixReportLevel.Group, counts.Group),
            (MatrixReportLevel.Kol, counts.Kol),
            (MatrixReportLevel.Moin, counts.Moin),
            (MatrixReportLevel.Tafsili1, counts.T1),
            (MatrixReportLevel.Tafsili2, counts.T2),
            (MatrixReportLevel.Tafsili3, counts.T3),
            (MatrixReportLevel.Tafsili4, counts.T4),
            (MatrixReportLevel.Tafsili5, counts.T5),
            (MatrixReportLevel.Tafsili6, counts.T6),
            (MatrixReportLevel.Tafsili7, counts.T7),
        };

        return pairs.Where(p => p.Count > 0).Select(p => p.Level).ToList();
    }

    private IQueryable<VW_CONSOLIDATE_REPORT> Filter(GetMatrixReportQuery query)
    {
        var rows = _dbContext.VW_CONSOLIDATE_REPORTs
            .AsNoTracking()
            // Compared as a number, never as a boolean — see VW_CONSOLIDATE_REPORT.ISDELETED.
            // `!= true` on a bool? renders `<> True`, which Oracle rejects with ORA-00904.
            .Where(v => v.ISDELETED == null || v.ISDELETED != 1)
            .Where(v => v.YEAR == query.Year)
            .Where(v => v.VAHEDCODE == query.VahedCode);

        // The drill-down path. Each step is one equality on the level's own column, which the view
        // has already flattened onto every line — so depth costs no joins.
        foreach (var step in query.Scope ?? Array.Empty<MatrixReportScopeItem>())
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
        MatrixReportLevel level,
        string code)
        => level switch
        {
            MatrixReportLevel.Group => v => v.GROUPCODE == code,
            MatrixReportLevel.Kol => v => v.KOLCODE == code,
            MatrixReportLevel.Moin => v => v.MOINCODE == code,
            MatrixReportLevel.Tafsili1 => v => v.TAFSILICODE1 == code,
            MatrixReportLevel.Tafsili2 => v => v.TAFSILICODE2 == code,
            MatrixReportLevel.Tafsili3 => v => v.TAFSILICODE3 == code,
            MatrixReportLevel.Tafsili4 => v => v.TAFSILICODE4 == code,
            MatrixReportLevel.Tafsili5 => v => v.TAFSILICODE5 == code,
            MatrixReportLevel.Tafsili6 => v => v.TAFSILICODE6 == code,
            MatrixReportLevel.Tafsili7 => v => v.TAFSILICODE7 == code,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown matrix report level."),
        };

    private static Expression<Func<VW_CONSOLIDATE_REPORT, LevelRow>> LevelProjection(MatrixReportLevel level)
        => level switch
        {
            MatrixReportLevel.Group => v => new LevelRow
            { Code = v.GROUPCODE, Name = v.GROUPNAME, NextCode = v.KOLCODE, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            MatrixReportLevel.Kol => v => new LevelRow
            { Code = v.KOLCODE, Name = v.KOLNAME, NextCode = v.MOINCODE, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            MatrixReportLevel.Moin => v => new LevelRow
            { Code = v.MOINCODE, Name = v.MOINNAME, NextCode = v.TAFSILICODE1, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            MatrixReportLevel.Tafsili1 => v => new LevelRow
            { Code = v.TAFSILICODE1, Name = v.TAFSILINAME1, NextCode = v.TAFSILICODE2, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            MatrixReportLevel.Tafsili2 => v => new LevelRow
            { Code = v.TAFSILICODE2, Name = v.TAFSILINAME2, NextCode = v.TAFSILICODE3, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            MatrixReportLevel.Tafsili3 => v => new LevelRow
            { Code = v.TAFSILICODE3, Name = v.TAFSILINAME3, NextCode = v.TAFSILICODE4, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            MatrixReportLevel.Tafsili4 => v => new LevelRow
            { Code = v.TAFSILICODE4, Name = v.TAFSILINAME4, NextCode = v.TAFSILICODE5, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            MatrixReportLevel.Tafsili5 => v => new LevelRow
            { Code = v.TAFSILICODE5, Name = v.TAFSILINAME5, NextCode = v.TAFSILICODE6, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            MatrixReportLevel.Tafsili6 => v => new LevelRow
            { Code = v.TAFSILICODE6, Name = v.TAFSILINAME6, NextCode = v.TAFSILICODE7, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            // Deepest level: nothing below it, so no row here can ever offer a drill-down.
            MatrixReportLevel.Tafsili7 => v => new LevelRow
            { Code = v.TAFSILICODE7, Name = v.TAFSILINAME7, NextCode = null, Debtor = v.DEBTOR, Creditor = v.CREDITOR },
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown matrix report level."),
        };

    /// <summary>Persian label for the level, shown as the row's «سطح» in the report.</summary>
    private static string LevelLabel(MatrixReportLevel level) => level switch
    {
        MatrixReportLevel.Group => "گروه",
        MatrixReportLevel.Kol => "کل",
        MatrixReportLevel.Moin => "معین",
        MatrixReportLevel.Tafsili1 => "تفصیلی ۱",
        MatrixReportLevel.Tafsili2 => "تفصیلی ۲",
        MatrixReportLevel.Tafsili3 => "تفصیلی ۳",
        MatrixReportLevel.Tafsili4 => "تفصیلی ۴",
        MatrixReportLevel.Tafsili5 => "تفصیلی ۵",
        MatrixReportLevel.Tafsili6 => "تفصیلی ۶",
        MatrixReportLevel.Tafsili7 => "تفصیلی ۷",
        _ => "تفصیلی",
    };
}
