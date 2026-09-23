using System.Linq.Expressions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.CrossTab;
using Accounting.Application.Reports.CrossTab.GetCrossTabReport;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ICrossTabReportReadRepository"/>: the pivot
/// behind گزارش متقاطع.
///
/// <para>
/// <b>One GROUP BY, no joins.</b> Every dimension the report offers is already a column of
/// <c>VW_CONSOLIDATE_REPORT</c>, so crossing any two of them is a single grouped aggregate over a
/// read model. The dimensions only decide which <see cref="Expression"/> the rows are projected
/// through, so every filter stays a parameterised LINQ predicate and no caller-supplied value ever
/// reaches SQL text — the same discipline as
/// <see cref="MatrixReportReadRepository"/>, and the reason this report needs no raw SQL at all.
/// </para>
///
/// <para>
/// ⚠️ The projections below must stay expressions EF can translate. Calling <c>.Compile()</c> on
/// one — the obvious-looking way to pick a column per dimension — would move the grouping into
/// memory and drag the whole view across the wire before aggregating.
/// </para>
///
/// <para>
/// ⚠️ <b>No boolean ever reaches the generated SQL.</b> Oracle has no boolean literal and this
/// project has been bitten by that twice (phases 37 and 40), which is why <c>ISDELETED</c> is
/// compared as the number it is rather than through a <c>bool?</c>.
/// </para>
/// </summary>
public sealed class CrossTabReportReadRepository : ICrossTabReportReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public CrossTabReportReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Dimension-independent shape the view rows are normalised into before grouping. Making the
    /// two dimensions a projection rather than a grouping key is what keeps the query translatable.
    /// </summary>
    private sealed class CrossTabFact
    {
        public string? RowCode { get; set; }

        public string? RowName { get; set; }

        public string? ColumnCode { get; set; }

        public string? ColumnName { get; set; }

        public decimal? Debtor { get; set; }

        public decimal? Creditor { get; set; }
    }

    public async Task<CrossTabResultDto> GetAsync(
        GetCrossTabReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var rows = Filter(query);

        var facts = rows.Select(Projection(query.RowDimension, query.ColumnDimension));

        // A line with no code on either axis is excluded, not grouped under an empty key: a
        // تفصیلی level a line was never assigned at is absence, not a category. With two axes an
        // unfiltered null would create BOTH a blank row and a blank column, which is why this is
        // applied to each side independently.
        facts = facts.Where(f =>
            f.RowCode != null && f.RowCode != "" &&
            f.ColumnCode != null && f.ColumnCode != "");

        if (!string.IsNullOrWhiteSpace(query.RowCodeFilter))
        {
            var prefix = query.RowCodeFilter;
            facts = facts.Where(f => f.RowCode!.StartsWith(prefix));
        }

        if (!string.IsNullOrWhiteSpace(query.ColumnCodeFilter))
        {
            var prefix = query.ColumnCodeFilter;
            facts = facts.Where(f => f.ColumnCode!.StartsWith(prefix));
        }

        var cells = await facts
            .GroupBy(f => new { f.RowCode, f.RowName, f.ColumnCode, f.ColumnName })
            .Select(g => new
            {
                g.Key.RowCode,
                g.Key.RowName,
                g.Key.ColumnCode,
                g.Key.ColumnName,
                Debtor = g.Sum(x => x.Debtor ?? 0m),
                Creditor = g.Sum(x => x.Creditor ?? 0m),
            })
            .ToListAsync(cancellationToken);

        // Everything below is over the materialised cell set, which is one row per populated
        // intersection — small by construction, and far cheaper to pivot in memory than to ask
        // Oracle for the same grouping three more times.
        var allColumns = cells
            .GroupBy(c => c.ColumnCode!, StringComparer.Ordinal)
            .Select(g => new CrossTabColumnDto(
                g.Key,
                g.Select(x => x.ColumnName).FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? string.Empty,
                g.Sum(x => x.Debtor),
                g.Sum(x => x.Creditor)))
            .OrderBy(c => c.Code, StringComparer.Ordinal)
            .ToList();

        var totalColumnCount = allColumns.Count;
        var truncated = totalColumnCount > GetCrossTabReportQueryValidator.MaxColumns;

        // Widest columns first when trimming: if the grid cannot show everything, the columns
        // worth keeping are the ones carrying the most turnover, not the ones that happen to sort
        // first. The kept set is then put back into code order so the header still reads naturally.
        var visibleColumns = truncated
            ? allColumns
                .OrderByDescending(c => c.Debtor + c.Creditor)
                .Take(GetCrossTabReportQueryValidator.MaxColumns)
                .OrderBy(c => c.Code, StringComparer.Ordinal)
                .ToList()
            : allColumns;

        var visibleCodes = visibleColumns.Select(c => c.Code).ToHashSet(StringComparer.Ordinal);

        var reportRows = cells
            .GroupBy(c => c.RowCode!, StringComparer.Ordinal)
            .Select(g => new CrossTabRowDto(
                g.Key,
                g.Select(x => x.RowName).FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? string.Empty,
                g.Where(x => visibleCodes.Contains(x.ColumnCode!))
                    .Select(x => new CrossTabCellDto(x.ColumnCode!, x.Debtor, x.Creditor))
                    .OrderBy(c => c.ColumnCode, StringComparer.Ordinal)
                    .ToList(),
                // Row totals span the row's WHOLE set of cells, including any hidden by the column
                // cap. A row total that silently excluded trimmed columns would disagree with the
                // trial balance for the same period, which is worse than a total the visible cells
                // do not add up to — the result flags the truncation so the client can say so.
                g.Sum(x => x.Debtor),
                g.Sum(x => x.Creditor)))
            .Where(r => r.Cells.Count > 0)
            .OrderBy(r => r.Code, StringComparer.Ordinal)
            .ToList();

        return new CrossTabResultDto(
            query.RowDimension,
            DimensionLabel(query.RowDimension),
            query.ColumnDimension,
            DimensionLabel(query.ColumnDimension),
            visibleColumns,
            reportRows,
            cells.Sum(c => c.Debtor),
            cells.Sum(c => c.Creditor),
            totalColumnCount,
            truncated);
    }

    private IQueryable<VW_CONSOLIDATE_REPORT> Filter(GetCrossTabReportQuery query)
    {
        var rows = _dbContext.VW_CONSOLIDATE_REPORTs
            .AsNoTracking()
            // Compared as a number, never as a boolean — `!= true` on a bool? renders `<> True`,
            // which Oracle rejects with ORA-00904.
            .Where(v => v.ISDELETED == null || v.ISDELETED != 1)
            .Where(v => v.YEAR == query.Year)
            .Where(v => v.VAHEDCODE == query.VahedCode);

        // VOUCHERDATE is fixed-width YYYYMMDD text in this schema, so an ordinal comparison is
        // chronological with no conversion needed.
        if (!string.IsNullOrWhiteSpace(query.FromDate))
        {
            rows = rows.Where(v => v.VOUCHERDATE != null && string.Compare(v.VOUCHERDATE, query.FromDate) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.ToDate))
        {
            rows = rows.Where(v => v.VOUCHERDATE != null && string.Compare(v.VOUCHERDATE, query.ToDate) <= 0);
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

    /// <summary>
    /// Builds the two-axis projection by composing the per-dimension code/name selectors.
    ///
    /// <para>
    /// Composed through <see cref="Expression"/> rather than written out as a switch over all 90
    /// ordered dimension pairs — which is the only alternative that stays translatable, and is
    /// unmaintainable. The property names come from a fixed switch over the enum, never from
    /// caller input, so nothing here can be steered by a request.
    /// </para>
    /// </summary>
    private static Expression<Func<VW_CONSOLIDATE_REPORT, CrossTabFact>> Projection(
        CrossTabDimension row,
        CrossTabDimension column)
    {
        var v = Expression.Parameter(typeof(VW_CONSOLIDATE_REPORT), "v");

        Expression Column(string name) => Expression.Property(v, name);

        var init = Expression.MemberInit(
            Expression.New(typeof(CrossTabFact)),
            Expression.Bind(typeof(CrossTabFact).GetProperty(nameof(CrossTabFact.RowCode))!, Column(CodeColumn(row))),
            Expression.Bind(typeof(CrossTabFact).GetProperty(nameof(CrossTabFact.RowName))!, Column(NameColumn(row))),
            Expression.Bind(typeof(CrossTabFact).GetProperty(nameof(CrossTabFact.ColumnCode))!, Column(CodeColumn(column))),
            Expression.Bind(typeof(CrossTabFact).GetProperty(nameof(CrossTabFact.ColumnName))!, Column(NameColumn(column))),
            Expression.Bind(typeof(CrossTabFact).GetProperty(nameof(CrossTabFact.Debtor))!, Column(nameof(VW_CONSOLIDATE_REPORT.DEBTOR))),
            Expression.Bind(typeof(CrossTabFact).GetProperty(nameof(CrossTabFact.Creditor))!, Column(nameof(VW_CONSOLIDATE_REPORT.CREDITOR))));

        return Expression.Lambda<Func<VW_CONSOLIDATE_REPORT, CrossTabFact>>(init, v);
    }

    /// <summary>Which view column carries this dimension's code. Fixed mapping — never caller input.</summary>
    private static string CodeColumn(CrossTabDimension dimension) => dimension switch
    {
        CrossTabDimension.Group => nameof(VW_CONSOLIDATE_REPORT.GROUPCODE),
        CrossTabDimension.Kol => nameof(VW_CONSOLIDATE_REPORT.KOLCODE),
        CrossTabDimension.Moin => nameof(VW_CONSOLIDATE_REPORT.MOINCODE),
        CrossTabDimension.Tafsili1 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE1),
        CrossTabDimension.Tafsili2 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE2),
        CrossTabDimension.Tafsili3 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE3),
        CrossTabDimension.Tafsili4 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE4),
        CrossTabDimension.Tafsili5 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE5),
        CrossTabDimension.Tafsili6 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE6),
        CrossTabDimension.Tafsili7 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE7),
        _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown cross-tab dimension."),
    };

    /// <summary>Which view column carries this dimension's title.</summary>
    private static string NameColumn(CrossTabDimension dimension) => dimension switch
    {
        CrossTabDimension.Group => nameof(VW_CONSOLIDATE_REPORT.GROUPNAME),
        CrossTabDimension.Kol => nameof(VW_CONSOLIDATE_REPORT.KOLNAME),
        CrossTabDimension.Moin => nameof(VW_CONSOLIDATE_REPORT.MOINNAME),
        CrossTabDimension.Tafsili1 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME1),
        CrossTabDimension.Tafsili2 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME2),
        CrossTabDimension.Tafsili3 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME3),
        CrossTabDimension.Tafsili4 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME4),
        CrossTabDimension.Tafsili5 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME5),
        CrossTabDimension.Tafsili6 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME6),
        CrossTabDimension.Tafsili7 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME7),
        _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown cross-tab dimension."),
    };

    /// <summary>Persian label for an axis.</summary>
    private static string DimensionLabel(CrossTabDimension dimension) => dimension switch
    {
        CrossTabDimension.Group => "گروه",
        CrossTabDimension.Kol => "کل",
        CrossTabDimension.Moin => "معین",
        CrossTabDimension.Tafsili1 => "تفصیلی ۱",
        CrossTabDimension.Tafsili2 => "تفصیلی ۲",
        CrossTabDimension.Tafsili3 => "تفصیلی ۳",
        CrossTabDimension.Tafsili4 => "تفصیلی ۴",
        CrossTabDimension.Tafsili5 => "تفصیلی ۵",
        CrossTabDimension.Tafsili6 => "تفصیلی ۶",
        CrossTabDimension.Tafsili7 => "تفصیلی ۷",
        _ => "تفصیلی",
    };
}
