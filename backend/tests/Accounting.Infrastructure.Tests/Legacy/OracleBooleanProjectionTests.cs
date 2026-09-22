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
