using Accounting.Application.Common.Security;
using System.Linq.Expressions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.MatrixReport;
using Accounting.Application.Reports.MatrixReport.GetMatrixReport;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IMatrixReportReadRepository"/>: the pivot
/// behind گزارش ماتریسی.
///
/// <para>
/// <b>One GROUP BY, no joins.</b> Every dimension the report offers is already a column of
/// <c>VW_CONSOLIDATE_REPORT</c>, so crossing any two of them is a single grouped aggregate over a
/// read model. The dimensions only decide which <see cref="Expression"/> the rows are projected
/// through, so every filter stays a parameterised LINQ predicate and no caller-supplied value ever
/// reaches SQL text — the same discipline as
/// <see cref="AccountReviewReadRepository"/>, and the reason this report needs no raw SQL at all.
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
public sealed class MatrixReportReadRepository : IMatrixReportReadRepository
{
    private readonly LegacyDbContext _dbContext;

    private readonly IReportUnitScope? _unitScope;

    public MatrixReportReadRepository(LegacyDbContext dbContext, IReportUnitScope? unitScope = null)
    {
        _dbContext = dbContext;
        _unitScope = unitScope;
    }

    /// <summary>
    /// Dimension-independent shape the view rows are normalised into before grouping. Making the
    /// two dimensions a projection rather than a grouping key is what keeps the query translatable.
    /// </summary>
    private sealed class MatrixFact
    {
        public string? RowCode { get; set; }

        public string? RowName { get; set; }

        public string? ColumnCode { get; set; }

        public string? ColumnName { get; set; }

        public decimal? Debtor { get; set; }

        public decimal? Creditor { get; set; }
    }

    public async Task<MatrixResultDto> GetAsync(
        GetMatrixReportQuery query,
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
            .Select(g => new MatrixColumnDto(
                g.Key,
                g.Select(x => x.ColumnName).FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? string.Empty,
                g.Sum(x => x.Debtor),
                g.Sum(x => x.Creditor)))
            .OrderBy(c => c.Code, StringComparer.Ordinal)
            .ToList();

        var totalColumnCount = allColumns.Count;
        var truncated = totalColumnCount > GetMatrixReportQueryValidator.MaxColumns;

        // Widest columns first when trimming: if the grid cannot show everything, the columns
        // worth keeping are the ones carrying the most turnover, not the ones that happen to sort
        // first. The kept set is then put back into code order so the header still reads naturally.
        var visibleColumns = truncated
            ? allColumns
                .OrderByDescending(c => c.Debtor + c.Creditor)
                .Take(GetMatrixReportQueryValidator.MaxColumns)
                .OrderBy(c => c.Code, StringComparer.Ordinal)
                .ToList()
            : allColumns;

        var visibleCodes = visibleColumns.Select(c => c.Code).ToHashSet(StringComparer.Ordinal);

        var reportRows = cells
            .GroupBy(c => c.RowCode!, StringComparer.Ordinal)
            .Select(g => new MatrixRowDto(
                g.Key,
                g.Select(x => x.RowName).FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? string.Empty,
                g.Where(x => visibleCodes.Contains(x.ColumnCode!))
                    .Select(x => new MatrixCellDto(x.ColumnCode!, x.Debtor, x.Creditor))
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

        return new MatrixResultDto(
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

    private IQueryable<VW_CONSOLIDATE_REPORT> Filter(GetMatrixReportQuery query)
    {
        var rows = _dbContext.VW_CONSOLIDATE_REPORTs
            .AsNoTracking()
            // Compared as a number, never as a boolean — `!= true` on a bool? renders `<> True`,
            // which Oracle rejects with ORA-00904.
            .Where(v => v.ISDELETED == null || v.ISDELETED != 1)
            .Where(v => v.YEAR == query.Year)
            // دامنهٔ چندواحدی (همهٔ واحدها / زیرمجموعه / گروه) — ReportUnitScopeBehavior؛ وگرنه فقط واحد جاری.
            .WhereVahed(v => v.VAHEDCODE, query.VahedCode, _unitScope?.VahedCodes);

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
    private static Expression<Func<VW_CONSOLIDATE_REPORT, MatrixFact>> Projection(
        MatrixDimension row,
        MatrixDimension column)
    {
        var v = Expression.Parameter(typeof(VW_CONSOLIDATE_REPORT), "v");

        Expression Column(string name) => Expression.Property(v, name);

        var init = Expression.MemberInit(
            Expression.New(typeof(MatrixFact)),
            Expression.Bind(typeof(MatrixFact).GetProperty(nameof(MatrixFact.RowCode))!, Column(CodeColumn(row))),
            Expression.Bind(typeof(MatrixFact).GetProperty(nameof(MatrixFact.RowName))!, Column(NameColumn(row))),
            Expression.Bind(typeof(MatrixFact).GetProperty(nameof(MatrixFact.ColumnCode))!, Column(CodeColumn(column))),
            Expression.Bind(typeof(MatrixFact).GetProperty(nameof(MatrixFact.ColumnName))!, Column(NameColumn(column))),
            Expression.Bind(typeof(MatrixFact).GetProperty(nameof(MatrixFact.Debtor))!, Column(nameof(VW_CONSOLIDATE_REPORT.DEBTOR))),
            Expression.Bind(typeof(MatrixFact).GetProperty(nameof(MatrixFact.Creditor))!, Column(nameof(VW_CONSOLIDATE_REPORT.CREDITOR))));

        return Expression.Lambda<Func<VW_CONSOLIDATE_REPORT, MatrixFact>>(init, v);
    }

    /// <summary>Which view column carries this dimension's code. Fixed mapping — never caller input.</summary>
    private static string CodeColumn(MatrixDimension dimension) => dimension switch
    {
        MatrixDimension.Group => nameof(VW_CONSOLIDATE_REPORT.GROUPCODE),
        MatrixDimension.Kol => nameof(VW_CONSOLIDATE_REPORT.KOLCODE),
        MatrixDimension.Moin => nameof(VW_CONSOLIDATE_REPORT.MOINCODE),
        MatrixDimension.Tafsili1 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE1),
        MatrixDimension.Tafsili2 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE2),
        MatrixDimension.Tafsili3 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE3),
        MatrixDimension.Tafsili4 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE4),
        MatrixDimension.Tafsili5 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE5),
        MatrixDimension.Tafsili6 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE6),
        MatrixDimension.Tafsili7 => nameof(VW_CONSOLIDATE_REPORT.TAFSILICODE7),
        _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown cross-tab dimension."),
    };

    /// <summary>Which view column carries this dimension's title.</summary>
    private static string NameColumn(MatrixDimension dimension) => dimension switch
    {
        MatrixDimension.Group => nameof(VW_CONSOLIDATE_REPORT.GROUPNAME),
        MatrixDimension.Kol => nameof(VW_CONSOLIDATE_REPORT.KOLNAME),
        MatrixDimension.Moin => nameof(VW_CONSOLIDATE_REPORT.MOINNAME),
        MatrixDimension.Tafsili1 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME1),
        MatrixDimension.Tafsili2 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME2),
        MatrixDimension.Tafsili3 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME3),
        MatrixDimension.Tafsili4 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME4),
        MatrixDimension.Tafsili5 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME5),
        MatrixDimension.Tafsili6 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME6),
        MatrixDimension.Tafsili7 => nameof(VW_CONSOLIDATE_REPORT.TAFSILINAME7),
        _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown cross-tab dimension."),
    };

    /// <summary>Persian label for an axis.</summary>
    private static string DimensionLabel(MatrixDimension dimension) => dimension switch
    {
        MatrixDimension.Group => "گروه",
        MatrixDimension.Kol => "کل",
        MatrixDimension.Moin => "معین",
        MatrixDimension.Tafsili1 => "تفصیلی ۱",
        MatrixDimension.Tafsili2 => "تفصیلی ۲",
        MatrixDimension.Tafsili3 => "تفصیلی ۳",
        MatrixDimension.Tafsili4 => "تفصیلی ۴",
        MatrixDimension.Tafsili5 => "تفصیلی ۵",
        MatrixDimension.Tafsili6 => "تفصیلی ۶",
        MatrixDimension.Tafsili7 => "تفصیلی ۷",
        _ => "تفصیلی",
    };
}
