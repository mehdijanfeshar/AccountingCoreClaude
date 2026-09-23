using Accounting.Domain.Entity;

namespace Accounting.Application.Tests.VahedTypes;

/// <summary>
/// <c>TB_VAHED_TYPE</c> is exposed read-only (see <c>IVahedTypeReadRepository</c> and
/// <c>Accounting.Api.Controllers.VahedTypesController</c>): organisational unit types arrive with
/// the shared <c>CENTRALACCOUNT</c> schema and are consumed by other systems on it, so this
/// project never creates, edits or deletes them.
///
/// This is an INTENTIONAL-ABSENCE GUARD, in the same family as
/// <see cref="VahedInfos.VahedInfoSchemaAssumptionsTests"/>. Each assertion locks in a schema fact
/// the read path relies on. If Oracle ever changes one of them the matching assertion fails, which
/// is the point: it forces whoever sees the failure to revisit the decision rather than silently
/// inheriting it.
/// </summary>
public sealed class VahedTypeSchemaAssumptionsTests
{
    /// <summary>
    /// The read repository applies no logical-delete filter. That is only correct while the table
    /// has no such column — otherwise it would start returning deleted unit types into the
    /// permission screen's picker.
    /// </summary>
    [Fact]
    public void TB_VAHED_TYPE_HasNoIsDeletedProperty()
    {
        Assert.Null(typeof(TB_VAHED_TYPE).GetProperty("ISDELETED"));
    }

    /// <summary>
    /// No audit columns, which is consistent with a table this project never writes to. If one
    /// appeared, it would be a signal that the table became writable somewhere.
    /// </summary>
    [Theory]
    [InlineData("ADDUSERID")]
    [InlineData("CHANGEUSERID")]
    [InlineData("CREATEDDATE")]
    [InlineData("UPDATEDDATE")]
    public void TB_VAHED_TYPE_HasNoAuditColumns(string propertyName)
    {
        Assert.Null(typeof(TB_VAHED_TYPE).GetProperty(propertyName));
    }

    /// <summary>
    /// No <c>VAHEDCODE</c> — this is a global lookup, which is why neither it nor the allow/deny
    /// list it feeds is unit-scoped. Recorded in <c>VahedScopeConventionTests</c>'s Group A.
    /// </summary>
    [Fact]
    public void TB_VAHED_TYPE_HasNoVahedCodeProperty()
    {
        Assert.Null(typeof(TB_VAHED_TYPE).GetProperty("VAHEDCODE"));
    }

    /// <summary>
    /// <c>PARENTTYPECODE</c> is what the «بخش» grouping level is built from, so the read path
    /// must keep exposing it. It is a plain string and is deliberately NOT a navigation property:
    /// live data rules out a self-reference (type 3 «بيمارستان» has parent "2", type 12 «خزانه»
    /// has parent "3" — neither names a plausible parent type), so it is a bare bucket id.
    /// </summary>
    [Fact]
    public void TB_VAHED_TYPE_ParentTypeCode_IsAPlainString_NotANavigation()
    {
        var property = typeof(TB_VAHED_TYPE).GetProperty("PARENTTYPECODE");

        Assert.NotNull(property);
        Assert.Equal(typeof(string), property!.PropertyType);
    }
}
