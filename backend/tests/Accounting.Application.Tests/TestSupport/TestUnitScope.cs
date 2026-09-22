using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.UnitAccess.Queries;
using Moq;

namespace Accounting.Application.Tests.TestSupport;

/// <summary>
/// Builds the <see cref="IUnitScopeResolver"/> that <c>VahedScopeBehavior</c> needs, for the many
/// handler tests that only care that the behavior stamps the caller's own unit onto a request.
///
/// <para>
/// <b>Deliberately wraps the REAL <see cref="UnitScopeResolver"/> rather than mocking the
/// interface.</b> Before phase 37-B those tests exercised the behavior's own fail-loud rules
/// (null claim → <c>MissingVahedScopeException</c>, over-length claim → same); that logic moved
/// into the resolver. Handing them a <c>Mock&lt;IUnitScopeResolver&gt;</c> would have silently
/// turned every one of those assertions into a test of the mock, so they keep the real rule and
/// only the unit-tree lookup is stubbed.
/// </para>
///
/// <para>
/// The stubbed repository <b>denies</b> every cross-unit request. These tests never set
/// <c>RequestedVahedCode</c> (a <c>Mock&lt;ICurrentUser&gt;</c> returns null for it), so the
/// resolver short-circuits to the caller's own unit and the repository is never consulted at all.
/// Denying rather than allowing means that if a future test does set a requested unit, it fails
/// loudly instead of quietly being granted access.
/// </para>
/// </summary>
internal static class TestUnitScope
{
    /// <summary>
    /// A resolver backed by <paramref name="currentUser"/> and a deny-everything unit tree.
    /// </summary>
    public static IUnitScopeResolver ResolverFor(ICurrentUser currentUser)
        => new UnitScopeResolver(currentUser, DenyAllUnitAccess());

    /// <summary>
    /// A resolver that grants <paramref name="allowedTargetVahedCode"/> in addition to the
    /// caller's own unit — for tests that deliberately exercise acting as another unit.
    /// </summary>
    public static IUnitScopeResolver ResolverAllowing(ICurrentUser currentUser, string allowedTargetVahedCode)
    {
        var unitAccess = new Mock<IUnitAccessReadRepository>();

        unitAccess
            .Setup(r => r.CanActAsAsync(
                It.IsAny<string>(),
                It.Is<string>(target => target == allowedTargetVahedCode),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        unitAccess
            .Setup(r => r.CanActAsAsync(
                It.IsAny<string>(),
                It.Is<string>(target => target != allowedTargetVahedCode),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        return new UnitScopeResolver(currentUser, unitAccess.Object);
    }

    private static IUnitAccessReadRepository DenyAllUnitAccess()
    {
        var unitAccess = new Mock<IUnitAccessReadRepository>();

        unitAccess
            .Setup(r => r.CanActAsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        unitAccess
            .Setup(r => r.GetAccessibleUnitsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AccessibleUnitDto>());

        return unitAccess.Object;
    }
}
