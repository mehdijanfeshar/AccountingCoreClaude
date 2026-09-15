using System.Reflection;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;

namespace Accounting.Application.Tests.PreDescribs;

/// <summary>
/// <c>TB_PREDESCRIB</c> is the one entity in this project's CRUD surface that deliberately gets
/// NO Delete command/handler/endpoint — because the table has no <c>ISDELETED</c> column, and
/// this project never issues physical deletes (see CLAUDE.md and
/// <c>Accounting.Api.Controllers.PreDescribsController</c> XML doc for the full rationale).
///
/// This is an INTENTIONAL-ABSENCE GUARD, not a coverage gap or a limitation to be "fixed"
/// later. Every assertion below locks in one of the schema facts that make Delete unsafe for
/// this entity. If Oracle ever gains an <c>ISDELETED</c> (or audit) column on
/// <c>TB_PREDESCRIBS</c>, the corresponding assertion here will start failing — which is by
/// design: it forces whoever notices the failure to consciously revisit whether a Delete path
/// should now be added, rather than someone "helpfully" adding
/// <c>DeletePreDescribCommand</c>/a delete endpoint without re-checking the schema first.
/// </summary>
public sealed class PreDescribSchemaAssumptionsTests
{
    [Fact]
    public void TB_PREDESCRIB_HasNoIsDeletedProperty()
    {
        // This is *the* reason there is no delete path: without ISDELETED, soft-delete
        // (the only delete mechanism this project allows) is not representable at all.
        var property = typeof(TB_PREDESCRIB).GetProperty("ISDELETED");

        Assert.Null(property);
    }

    [Fact]
    public void TB_PREDESCRIB_HasNoCreatedDateProperty()
    {
        var property = typeof(TB_PREDESCRIB).GetProperty("CREATEDDATE");

        Assert.Null(property);
    }

    [Fact]
    public void TB_PREDESCRIB_HasNoUpdatedDateProperty()
    {
        var property = typeof(TB_PREDESCRIB).GetProperty("UPDATEDDATE");

        Assert.Null(property);
    }

    [Fact]
    public void TB_PREDESCRIB_HasNoChangeUserIdProperty()
    {
        var property = typeof(TB_PREDESCRIB).GetProperty("CHANGEUSERID");

        Assert.Null(property);
    }

    [Fact]
    public void ApplicationAssembly_ContainsNoDeletePreDescribCommandType()
    {
        var applicationAssembly = typeof(IUnitOfWork).Assembly;

        var matchingType = applicationAssembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "DeletePreDescribCommand");

        Assert.Null(matchingType);
    }

    [Fact]
    public void IPreDescribRepository_ExposesNoMethodNamedWithDelete()
    {
        var methodNames = typeof(IPreDescribRepository)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name);

        Assert.DoesNotContain(methodNames, name => name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
    }
}
