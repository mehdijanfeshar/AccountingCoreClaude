using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Moq;

namespace Accounting.Application.Tests.Common.Security;

/// <summary>
/// The phase-37-B decision table: which unit a request actually runs as.
///
/// This is the one place untrusted client input (the <c>X-Vahed-Code</c> header) can influence a
/// row scope, so every branch is pinned here — especially the negative ones.
/// </summary>
public sealed class UnitScopeResolverTests
{
    private static Mock<ICurrentUser> User(string? ownVahedCode, string? requestedVahedCode = null)
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.VahedCode).Returns(ownVahedCode);
        currentUser.SetupGet(u => u.RequestedVahedCode).Returns(requestedVahedCode);
        return currentUser;
    }

    private static Mock<IUnitAccessReadRepository> UnitAccess(bool canActAs)
    {
        var unitAccess = new Mock<IUnitAccessReadRepository>();
        unitAccess
            .Setup(r => r.CanActAsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(canActAs);
        return unitAccess;
    }

    [Fact]
    public async Task No_unit_requested_resolves_to_the_callers_own_unit()
    {
        var resolver = new UnitScopeResolver(User("0042").Object, UnitAccess(canActAs: false).Object);

        Assert.Equal("0042", await resolver.ResolveEffectiveVahedCodeAsync());
    }

    [Fact]
    public async Task No_unit_requested_never_consults_the_unit_tree()
    {
        // The default path is every existing client and every request that does not switch units.
        // It must stay a pure claim read — no database work at all.
        var unitAccess = UnitAccess(canActAs: false);
        var resolver = new UnitScopeResolver(User("0042").Object, unitAccess.Object);

        await resolver.ResolveEffectiveVahedCodeAsync();

        unitAccess.Verify(
            r => r.CanActAsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Requesting_your_own_unit_short_circuits_without_a_tree_lookup()
    {
        var unitAccess = UnitAccess(canActAs: false);
        var resolver = new UnitScopeResolver(User("0042", requestedVahedCode: "0042").Object, unitAccess.Object);

        Assert.Equal("0042", await resolver.ResolveEffectiveVahedCodeAsync());

        unitAccess.Verify(
            r => r.CanActAsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Requesting_a_permitted_unit_resolves_to_that_unit()
    {
        var resolver = new UnitScopeResolver(
            User("1000", requestedVahedCode: "1100").Object,
            UnitAccess(canActAs: true).Object);

        Assert.Equal("1100", await resolver.ResolveEffectiveVahedCodeAsync());
    }

    [Fact]
    public async Task Requesting_a_forbidden_unit_throws_and_never_returns_it()
    {
        var resolver = new UnitScopeResolver(
            User("1000", requestedVahedCode: "2000").Object,
            UnitAccess(canActAs: false).Object);

        var ex = await Assert.ThrowsAsync<UnitActAsDeniedException>(
            () => resolver.ResolveEffectiveVahedCodeAsync());

        Assert.Equal("2000", ex.RequestedVahedCode);
        Assert.Equal("1000", ex.CallerVahedCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Missing_or_blank_own_claim_throws_regardless_of_what_was_requested(string? ownVahedCode)
    {
        var resolver = new UnitScopeResolver(
            User(ownVahedCode, requestedVahedCode: "1100").Object,
            UnitAccess(canActAs: true).Object);

        // Even with a unit tree that would grant anything, no usable claim means no scope. A
        // caller must never be able to acquire a scope purely by asking for one.
        await Assert.ThrowsAsync<MissingVahedScopeException>(
            () => resolver.ResolveEffectiveVahedCodeAsync());
    }

    [Fact]
    public async Task Over_length_own_claim_throws_rather_than_truncating()
    {
        var resolver = new UnitScopeResolver(User("00421").Object, UnitAccess(canActAs: true).Object);

        await Assert.ThrowsAsync<MissingVahedScopeException>(
            () => resolver.ResolveEffectiveVahedCodeAsync());
    }

    [Fact]
    public async Task Over_length_requested_unit_is_rejected_before_any_tree_lookup()
    {
        // VAHEDCODE is HasMaxLength(4) everywhere in Legacy. An over-long value can never match a
        // row, so it is refused here rather than being handed to a query.
        var unitAccess = UnitAccess(canActAs: true);
        var resolver = new UnitScopeResolver(User("1000", requestedVahedCode: "11000").Object, unitAccess.Object);

        await Assert.ThrowsAsync<UnitActAsDeniedException>(
            () => resolver.ResolveEffectiveVahedCodeAsync());

        unitAccess.Verify(
            r => r.CanActAsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Blank_requested_unit_falls_back_to_own_rather_than_failing()
    {
        // A client that sends the header with an empty value has asked for nothing, not for
        // something invalid — treat it as absent.
        var resolver = new UnitScopeResolver(
            User("0042", requestedVahedCode: "   ").Object,
            UnitAccess(canActAs: false).Object);

        Assert.Equal("0042", await resolver.ResolveEffectiveVahedCodeAsync());
    }

    [Fact]
    public async Task The_caller_own_unit_is_what_is_checked_against_not_the_requested_one()
    {
        // Guards against the subtle inversion of passing the requested unit as the "own" argument,
        // which would let any caller authorise themselves.
        var unitAccess = new Mock<IUnitAccessReadRepository>();
        unitAccess
            .Setup(r => r.CanActAsAsync("1000", "1100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resolver = new UnitScopeResolver(
            User("1000", requestedVahedCode: "1100").Object,
            unitAccess.Object);

        Assert.Equal("1100", await resolver.ResolveEffectiveVahedCodeAsync());

        unitAccess.Verify(r => r.CanActAsAsync("1000", "1100", It.IsAny<CancellationToken>()), Times.Once);
    }
}
