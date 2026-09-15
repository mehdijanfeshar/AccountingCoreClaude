using Accounting.Application.AccountCodes.Queries.GetTafsiliLevelItems;
using Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;
using Accounting.Application.Common.Security;

namespace Accounting.Application.Tests.AccountCodes.Queries;

/// <summary>
/// Structural guard, in the same spirit as <c>VahedScopeConventionTests</c>: pins down that
/// <see cref="GetTafsiliLevelItemsQuery"/> IS <see cref="IVahedScopedQuery"/> (Rule B needs the
/// caller's own unit code) and <see cref="GetTafsiliLevelsQuery"/> is NOT (the chart-of-accounts
/// level chain has no VAHEDCODE column anywhere), so a future refactor cannot silently flip
/// either decision without a test failing.
/// </summary>
public sealed class TafsiliLookupScopeTests
{
    [Fact]
    public void GetTafsiliLevelItemsQuery_ImplementsIVahedScopedQuery()
    {
        Assert.True(typeof(IVahedScopedQuery).IsAssignableFrom(typeof(GetTafsiliLevelItemsQuery)));
    }

    [Fact]
    public void GetTafsiliLevelsQuery_DoesNotImplementIVahedScoped()
    {
        Assert.False(typeof(IVahedScoped).IsAssignableFrom(typeof(GetTafsiliLevelsQuery)));
    }
}
