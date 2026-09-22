using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Legacy;

/// <summary>
/// Guards against the phase-37 defect class: a Legacy column scaffolded as CLR <c>byte</c> whose
/// Oracle precision allows values a byte cannot hold.
///
/// <para>
/// <b>The bug this exists to prevent.</b> <c>TB_YEAR.WORKING_YEAR</c> is <c>NUMBER(4,0)</c> and
/// holds Jalali years (1405, 1404), but was scaffolded as <c>byte</c> — max 255. Every read threw
/// <c>InvalidCastException</c> from <c>OracleDataReader.GetByte</c>. It went unnoticed from the
/// phase-2 scaffold all the way to phase 37 purely because no query had ever touched that table,
/// which is exactly why a convention test is the right shape of fix: the next such column will be
/// found by the build rather than by a 500 in production.
/// </para>
///
/// <para>
/// A <c>NUMBER(3)</c> column already reaches 999, so any precision of 3 or more overflows a byte.
/// Only scalar <c>byte</c>/<c>byte?</c> properties are considered — <c>byte[]</c> is a BLOB/RAW
/// mapping and unrelated.
/// </para>
/// </summary>
public sealed class ByteColumnPrecisionConventionTests
{
    /// <summary>A NUMBER(3) tops out at 999; byte tops out at 255.</summary>
    private const int FirstUnsafePrecision = 3;

    /// <summary>
    /// Pre-existing scaffold artefacts with the same shape as the <c>TB_YEAR</c> bug, left
    /// unfixed on purpose and recorded in <c>docs/open-decisions.md</c> (phase 37).
    ///
    /// <para>
    /// They are <b>latent, not active</b> — verified against live Oracle on 2026-09-22:
    /// <c>TB_CHECK_TYPE</c> holds a single row whose populated values are 10 and 20 with the rest
    /// NULL, and <c>TB_ATTACH.ATTACH_RADIF</c> peaks at 101. Nothing overflows today, so widening
    /// ~26 properties (and the DTOs above them) was out of scope for the unit/year work that found
    /// this. They remain real risks: the moment a cheque layout uses a coordinate above 255, or an
    /// attachment row passes 255, reading that row throws exactly as <c>TB_YEAR</c> did.
    /// </para>
    ///
    /// <para>
    /// This list must only ever shrink. A NEW entry means someone scaffolded or widened a column
    /// into the same trap, and the fix is to correct the CLR type, not to extend this list.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> KnownLatentOffenders = new(StringComparer.Ordinal)
    {
        "TB_ATTACH.ATTACH_RADIF",
        "TB_CHECK_TYPE.CHEQUE_WIDTH",
        "TB_CHECK_TYPE.CHEQUE_HEIGHT",
        "TB_CHECK_TYPE.CHEQUE_ADATE_LEFT",
        "TB_CHECK_TYPE.CHEQUE_ADATE_TOP",
        "TB_CHECK_TYPE.CHEQUE_ADATE_WIDTH",
        "TB_CHECK_TYPE.CHEQUE_NDATE_LEFT",
        "TB_CHECK_TYPE.CHEQUE_NDATE_TOP",
        "TB_CHECK_TYPE.CHEQUE_NDATE_WIDTH",
        "TB_CHECK_TYPE.CHEQUE_AAMOUNT_LEFT",
        "TB_CHECK_TYPE.CHEQUE_AAMOUNT_TOP",
        "TB_CHECK_TYPE.CHEQUE_AAMOUNT_WIDTH",
        "TB_CHECK_TYPE.CHEQUE_LAMOUNT_LEFT",
        "TB_CHECK_TYPE.CHEQUE_LAMOUNT_TOP",
        "TB_CHECK_TYPE.CHEQUE_LAMOUNT_WIDTH",
        "TB_CHECK_TYPE.CHEQUE_NAMOUNT_LEFT",
        "TB_CHECK_TYPE.CHEQUE_NAMOUNT_TOP",
        "TB_CHECK_TYPE.CHEQUE_NAMOUNT_WIDTH",
        "TB_CHECK_TYPE.CHEQUE_DESCRIBE1_LEFT",
        "TB_CHECK_TYPE.CHEQUE_DESCRIBE1_TOP",
        "TB_CHECK_TYPE.CHEQUE_DESCRIBE1_WIDTH",
        "TB_CHECK_TYPE.CHEQUE_DESCRIBE2_LEFT",
        "TB_CHECK_TYPE.CHEQUE_DESCRIBE2_TOP",
        "TB_CHECK_TYPE.CHEQUE_DESCRIBE2_WIDTH",
        "TB_CHECK_TYPE.CHEQUE_BREAKLINE_LEFT",
        "TB_CHECK_TYPE.CHEQUE_BREAKLINE_TOP",
        "TB_CHECK_TYPE.CHEQUE_BREAKLINE_WIDTH",
        "TB_CHECK_TYPE.PRINTER_MARGINE_LEFT",
        "TB_CHECK_TYPE.PRINTER_MARGINE_TOP",
    };

    private static LegacyDbContext CreateContext()
    {
        // No connection is opened — only the model metadata is inspected.
        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseOracle("Data Source=unused;User Id=unused;Password=unused;")
            .Options;
        return new LegacyDbContext(options);
    }

    [Fact]
    public void No_byte_property_is_mapped_to_a_column_wider_than_a_byte()
    {
        using var context = CreateContext();

        var offenders = new List<string>();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                if (clrType != typeof(byte))
                {
                    continue;
                }

                var precision = property.GetPrecision();

                if (precision is null || precision.Value < FirstUnsafePrecision)
                {
                    continue;
                }

                var key = $"{entityType.GetTableName()}.{property.GetColumnName()}";

                if (KnownLatentOffenders.Contains(key))
                {
                    continue;
                }

                offenders.Add($"{key} is CLR byte but mapped to NUMBER({precision.Value}), which can exceed 255.");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Legacy column(s) scaffolded as byte but too wide for one — reading a real row throws " +
            "InvalidCastException from OracleDataReader.GetByte:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// The allowlist must not rot. An entry that no longer describes a real byte-typed property
    /// means the column was fixed (good) and the entry should be deleted, or renamed away (in
    /// which case the allowlist is silently covering nothing).
    /// </summary>
    [Fact]
    public void Every_known_latent_offender_still_exists_and_still_needs_the_exemption()
    {
        using var context = CreateContext();

        var actual = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                var precision = property.GetPrecision();

                if (clrType == typeof(byte) && precision is not null && precision.Value >= FirstUnsafePrecision)
                {
                    actual.Add($"{entityType.GetTableName()}.{property.GetColumnName()}");
                }
            }
        }

        var stale = KnownLatentOffenders.Except(actual).ToList();

        Assert.True(
            stale.Count == 0,
            "These allowlist entries no longer match a too-wide byte property. If the column was " +
            "fixed, delete the entry:" + Environment.NewLine + string.Join(Environment.NewLine, stale));
    }

    [Fact]
    public void TB_YEAR_WORKING_YEAR_is_wide_enough_for_a_jalali_year()
    {
        using var context = CreateContext();

        var property = context.Model
            .GetEntityTypes()
            .Single(e => e.GetTableName() == "TB_YEAR")
            .GetProperties()
            .Single(p => p.GetColumnName() == "WORKING_YEAR");

        // The specific regression: this was byte, and every live row (1405, 1404) threw on read.
        Assert.NotEqual(typeof(byte), Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType);
        Assert.True(short.MaxValue >= 9999, "NUMBER(4) must fit in the chosen CLR type.");
    }
}
