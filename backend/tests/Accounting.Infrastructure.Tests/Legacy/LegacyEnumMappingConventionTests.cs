using Accounting.Infrastructure.Legacy;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Tests.Legacy;

/// <summary>
/// Locks in the mapping rule that phase 25 had to discover the hard way, on live Oracle, after a
/// correctly-typed enum property still blew up with <c>InvalidCastException</c> inside a predicate.
///
/// The Oracle provider picks its <c>bool</c> mapping from the STORE type name, not the CLR type.
/// So a <c>NUMBER(1)</c> column whose property is an enum is still routed through the provider's
/// boolean mapping unless the mapping does both of these:
/// <list type="number">
///   <item>declares an explicit <c>.HasConversion&lt;int&gt;()</c> / <c>&lt;int?&gt;()</c>, and</item>
///   <item>drops the <c>.HasColumnType("NUMBER(1)")</c> the scaffolder emitted.</item>
/// </list>
///
/// Every enum column converted in phases 21/25/27 and after follows that rule; these two tests
/// make it a convention rather than a thing each future conversion has to remember. They inspect
/// the built model's metadata, so the provider used to build it is irrelevant — the converter and
/// the column type are both model-level annotations.
///
/// ⚠️ Deliberately NOT asserted here: that the enum's VALUES match the legacy column's real
/// domain. Only the reference project plus live <c>CENTRALACCOUNT</c> data can settle that, and
/// several such conversions are still recorded as unverified in <c>docs/open-decisions.md</c>.
/// </summary>
public sealed class LegacyEnumMappingConventionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LegacyDbContext _context;

    public LegacyEnumMappingConventionTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LegacyDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LegacyDbContext(options);
    }

    [Fact]
    public void Every_enum_property_declares_an_explicit_integer_value_converter()
    {
        // NOTE: GetValueConverter() is the wrong question to ask here — it returns null for
        // .HasConversion<int>(), because passing a TYPE (rather than a ValueConverter instance)
        // records a provider CLR type and lets the type mapping supply the converter. Asking the
        // wrong way makes every correctly-mapped enum look like an offender.
        var offenders = EnumProperties()
            .Where(p =>
            {
                var providerType = p.Property.GetProviderClrType();
                if (providerType is null)
                {
                    return p.Property.GetValueConverter() is null;
                }

                return (Nullable.GetUnderlyingType(providerType) ?? providerType) != typeof(int);
            })
            .Select(p => p.Description)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "An enum property without an explicit .HasConversion<int>()/<int?>() is mapped by the "
            + "Oracle provider's bool mapping when the column is NUMBER(1), which throws "
            + "InvalidCastException the moment the property is used in a predicate (phase 25). "
            + $"Offenders: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void No_enum_property_keeps_the_scaffolded_NUMBER1_store_type()
    {
        var offenders = EnumProperties()
            .Where(p => string.Equals(
                p.Property.GetColumnType(), "NUMBER(1)", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Description)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "The Oracle provider decides between its bool and numeric mappings by store type name, "
            + "so a NUMBER(1) column type defeats the value converter above (phase 25). "
            + $"Offenders: {string.Join(", ", offenders)}");
    }

    private IEnumerable<(Microsoft.EntityFrameworkCore.Metadata.IProperty Property, string Description)> EnumProperties()
    {
        foreach (var entityType in _context.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!clrType.IsEnum)
                {
                    continue;
                }

                yield return (property, $"{entityType.ClrType.Name}.{property.Name} ({clrType.Name})");
            }
        }
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
