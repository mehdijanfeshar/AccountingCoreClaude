using System.Reflection;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;

namespace Accounting.Application.Tests.VahedInfos;

/// <summary>
/// <c>TB_VAHED_INFO</c> is one of the entities in this project's CRUD surface that
/// deliberately gets NO Delete command/handler/endpoint — because the table has no
/// <c>ISDELETED</c> column, and this project never issues physical deletes (see CLAUDE.md and
/// <c>Accounting.Api.Controllers.VahedInfosController</c> XML doc for the full rationale).
///
/// It is ALSO the one entity in this batch with NO audit columns at all — no
/// <c>ADDUSERID</c>, <c>CHANGEUSERID</c>, <c>CREATEDDATE</c> or <c>UPDATEDDATE</c> — which is
/// why its Create/Update handlers never depend on <c>ICurrentUser</c>. Writes to this table
/// therefore leave no audit trail whatsoever; that is a recorded schema gap, not something
/// this test suite invents a workaround for.
///
/// This is an INTENTIONAL-ABSENCE GUARD, not a coverage gap or a limitation to be "fixed"
/// later (mirrors <c>PreDescribSchemaAssumptionsTests</c>). Every assertion below locks in one
/// of the schema facts that make Delete unsafe/unrepresentable and audit stamping impossible
/// for this entity. If Oracle ever gains an <c>ISDELETED</c> (or audit) column on
/// <c>TB_VAHED_INFO</c>, the corresponding assertion here will start failing — which is by
/// design: it forces whoever notices the failure to consciously revisit whether a Delete path
/// or audit stamping should now be added, rather than someone "helpfully" adding
/// <c>DeleteVahedInfoCommand</c>/an <c>ICurrentUser</c> dependency without re-checking the
/// schema first.
/// </summary>
public sealed class VahedInfoSchemaAssumptionsTests
{
    [Fact]
    public void TB_VAHED_INFO_HasNoIsDeletedProperty()
    {
        // This is *the* reason there is no delete path: without ISDELETED, soft-delete
        // (the only delete mechanism this project allows) is not representable at all.
        var property = typeof(TB_VAHED_INFO).GetProperty("ISDELETED");

        Assert.Null(property);
    }

    [Fact]
    public void TB_VAHED_INFO_HasNoAddUserIdProperty()
    {
        var property = typeof(TB_VAHED_INFO).GetProperty("ADDUSERID");

        Assert.Null(property);
    }

    [Fact]
    public void TB_VAHED_INFO_HasNoChangeUserIdProperty()
    {
        var property = typeof(TB_VAHED_INFO).GetProperty("CHANGEUSERID");

        Assert.Null(property);
    }

    [Fact]
    public void TB_VAHED_INFO_HasNoCreatedDateProperty()
    {
        var property = typeof(TB_VAHED_INFO).GetProperty("CREATEDDATE");

        Assert.Null(property);
    }

    [Fact]
    public void TB_VAHED_INFO_HasNoUpdatedDateProperty()
    {
        var property = typeof(TB_VAHED_INFO).GetProperty("UPDATEDDATE");

        Assert.Null(property);
    }

    [Fact]
    public void ApplicationAssembly_ContainsNoDeleteVahedInfoCommandType()
    {
        var applicationAssembly = typeof(IUnitOfWork).Assembly;

        var matchingType = applicationAssembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "DeleteVahedInfoCommand");

        Assert.Null(matchingType);
    }

    [Fact]
    public void IVahedInfoRepository_ExposesNoMethodNamedWithDelete()
    {
        var methodNames = typeof(IVahedInfoRepository)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name);

        Assert.DoesNotContain(methodNames, name => name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
    }
}
