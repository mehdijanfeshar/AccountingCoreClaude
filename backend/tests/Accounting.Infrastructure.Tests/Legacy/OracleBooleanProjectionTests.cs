using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Accounting.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Legacy;

/// <summary>
/// Guards the phase-37 defect: projecting a boolean <i>expression</i> in a query against Oracle.
///
/// <para>
/// <b>The bug.</b> <c>UnitAccessReadRepository</c> originally selected
/// <c>IsHeadquarters = v.VAHEDTYPE.TYPECODE == "17"</c>. Oracle has no boolean type, so the
/// provider renders that projection with <c>TRUE</c>/<c>FALSE</c> literals and the server rejects
/// the statement with <c>ORA-00904: "FALSE": invalid identifier</c>. Every unit switch 500'd.
/// </para>
///
/// <para>
/// <b>Why this test exists in this unusual shape.</b> Every other repository test in this project
/// runs on SQLite, which accepts boolean literals happily — SQLite can <i>never</i> catch this
/// class of bug, and neither can a mocked handler test. What it needs is the real Oracle provider
/// generating SQL, which <see cref="EntityFrameworkQueryableExtensions.ToQueryString"/> does
/// without ever opening a connection. That makes the check cheap and offline while still being a
/// genuine test of Oracle translation.
/// </para>
///
/// <para>
/// The standing rule this encodes: <b>select the value, decide in C#</b>. Never project a
/// comparison.
/// </para>
/// </summary>
public sealed class OracleBooleanProjectionTests
{
    private static LegacyDbContext CreateOracleContext()
    {
        // Never connected to — ToQueryString() only needs the provider's SQL generator.
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseOracle("Data Source=unused;User Id=unused;Password=unused;")
            .Options;
        return new LegacyDbContext(options);
    }

    /// <summary>
    /// Reproduces the exact original mistake and proves it really does emit the literal that
    /// Oracle rejects. Without this, the "fixed" test below could pass for the wrong reason.
    /// </summary>
    [Fact]
    public void Projecting_a_boolean_comparison_emits_a_literal_oracle_rejects()
    {
        using var context = CreateOracleContext();

        var sql = context.TB_VAHED_INFOs
            .AsNoTracking()
            .Where(v => v.VAHEDCODE == "0000")
            .Select(v => new
            {
                v.ID,
                IsHeadquarters = v.VAHEDTYPE.TYPECODE == UnitAccessReadRepository.HeadquartersVahedTypeCode,
            })
            .ToQueryString();

        Assert.Contains("FALSE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_shipped_unit_lookup_projection_emits_no_boolean_literal()
    {
        using var context = CreateOracleContext();

        // The shape UnitAccessReadRepository actually uses now: raw TYPECODE, compared in C#.
        var sql = context.TB_VAHED_INFOs
            .AsNoTracking()
            .Where(v => v.VAHEDCODE == "0000")
            .Select(v => new { v.ID, TypeCode = v.VAHEDTYPE.TYPECODE })
            .ToQueryString();

        AssertNoBooleanLiteral(sql);
    }

    [Fact]
    public void The_shipped_unit_profile_projection_emits_no_boolean_literal()
    {
        using var context = CreateOracleContext();

        var sql = context.TB_VAHED_INFOs
            .AsNoTracking()
            .Where(v => v.VAHEDCODE == "0000")
            .Select(v => new { v.VAHEDNAME, TypeCode = v.VAHEDTYPE.TYPECODE })
            .ToQueryString();

        AssertNoBooleanLiteral(sql);
    }

    [Fact]
    public void The_shipped_accessible_units_projection_emits_no_boolean_literal()
    {
        using var context = CreateOracleContext();

        var sql = context.TB_VAHED_INFOs
            .AsNoTracking()
            .Select(v => new { v.ID, v.VAHEDCODE, v.VAHEDNAME, v.PARENT_ID, TypeCode = v.VAHEDTYPE.TYPECODE })
            .ToQueryString();

        AssertNoBooleanLiteral(sql);
    }

    /// <summary>
    /// The predicate half of the same defect, and the half this class originally missed.
    ///
    /// <para>
    /// Phase 37 fixed a projected boolean and guarded only projections. Phase 40 then shipped
    /// <c>Where(v =&gt; v.ISDELETED != true)</c> against the view and hit
    /// <c>ORA-00904: "TRUE": invalid identifier</c> at runtime — a comparison operand renders a
    /// literal just as readily as a projection does.
    /// </para>
    ///
    /// <para>
    /// <b>The precise root cause, which is narrower than "never use bool".</b> Every Legacy TABLE
    /// maps its <c>ISDELETED</c> with <c>HasColumnType("NUMBER(1)")</c>, and the Oracle provider's
    /// "NUMBER(1) ⇒ bool" convention (the phase-25 finding) then gives it a numeric store type, so
    /// <c>!= true</c> on those renders as a number comparison and is safe — this test proves that.
    /// The view entity declared a bare <c>bool?</c> with no store type at all, leaving the provider
    /// nothing to map to and a literal as its fallback. It is the mirror image of phase 25: there
    /// the store type forced bool when we wanted int, here its absence produced a literal.
    /// </para>
    /// </summary>
    [Fact]
    public void A_legacy_table_bool_is_safe_because_its_store_type_is_declared()
    {
        using var context = CreateOracleContext();

        var sql = context.TB_VOUCHERSHEADs
            .AsNoTracking()
            .Where(v => v.ISDELETED != true)
            .Select(v => v.ID)
            .ToQueryString();

        AssertNoBooleanLiteral(sql);
    }

    [Fact]
    public void The_shipped_matrix_report_predicate_emits_no_boolean_literal()
    {
        using var context = CreateOracleContext();

        // The shape MatrixReportReadRepository actually uses now: ISDELETED read and compared as a
        // number, so nothing boolean reaches the SQL.
        var sql = context.VW_CONSOLIDATE_REPORTs
            .AsNoTracking()
            .Where(v => v.ISDELETED == null || v.ISDELETED != 1)
            .Where(v => v.YEAR == "1404")
            .Select(v => new { v.MOINCODE, v.MOINNAME, v.DEBTOR, v.CREDITOR })
            .ToQueryString();

        AssertNoBooleanLiteral(sql);
    }

    [Fact]
    public void The_shipped_year_projection_emits_no_boolean_literal()
    {
        using var context = CreateOracleContext();

        var sql = context.TB_YEARs
            .AsNoTracking()
            .OrderByDescending(y => y.WORKING_YEAR)
            .Select(y => new { y.WORKING_YEAR, y.ISCURRENT, y.LAST_NUMBER })
            .ToQueryString();

        AssertNoBooleanLiteral(sql);
    }

    /// <summary>
    /// Matches <c>TRUE</c>/<c>FALSE</c> only as standalone words, so a column or alias that merely
    /// contains those letters does not trip the assertion.
    /// </summary>
    private static void AssertNoBooleanLiteral(string sql)
    {
        var offending = System.Text.RegularExpressions.Regex.Matches(
            sql,
            @"\b(TRUE|FALSE)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        Assert.True(
            offending.Count == 0,
            $"Generated Oracle SQL contains a boolean literal, which Oracle rejects with ORA-00904. " +
            $"Select the value and compare in C# instead.{Environment.NewLine}{sql}");
    }
}
