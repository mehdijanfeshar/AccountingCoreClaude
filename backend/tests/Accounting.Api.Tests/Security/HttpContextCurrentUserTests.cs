using System.Security.Claims;
using Accounting.Api.Security;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Accounting.Api.Tests.Security;

/// <summary>
/// Unit tests for <see cref="HttpContextCurrentUser"/>. Builds a <see cref="DefaultHttpContext"/>
/// directly and drives it through a mocked <see cref="IHttpContextAccessor"/> — no
/// <c>WebApplicationFactory</c>, no network, no database.
/// </summary>
public sealed class HttpContextCurrentUserTests
{
    private static HttpContextCurrentUser CreateSut(HttpContext? httpContext)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(httpContext);
        return new HttpContextCurrentUser(accessor.Object);
    }

    private static DefaultHttpContext AuthenticatedContext(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    [Fact]
    public void IsAuthenticated_ReturnsTrue_WhenPrincipalHasAuthenticationType()
    {
        var httpContext = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, "user1"));
        var sut = CreateSut(httpContext);

        Assert.True(sut.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenNoHttpContext()
    {
        var sut = CreateSut(httpContext: null);

        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenPrincipalIsAnonymous()
    {
        var httpContext = new DefaultHttpContext();
        var sut = CreateSut(httpContext);

        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public void UserId_ReturnsNameIdentifierClaim_WhenAuthenticatedAndWithinLengthLimit()
    {
        var httpContext = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, "user12345"));
        var sut = CreateSut(httpContext);

        Assert.Equal("user12345", sut.UserId);
    }

    [Fact]
    public void UserId_Throws_WhenUnauthenticated()
    {
        var httpContext = new DefaultHttpContext();
        var sut = CreateSut(httpContext);

        Assert.Throws<InvalidOperationException>(() => sut.UserId);
    }

    [Fact]
    public void UserId_Throws_WhenNoHttpContext()
    {
        var sut = CreateSut(httpContext: null);

        Assert.Throws<InvalidOperationException>(() => sut.UserId);
    }

    [Fact]
    public void UserId_Throws_WhenAuthenticatedButNoNameIdentifierClaim()
    {
        var httpContext = AuthenticatedContext(); // no claims at all
        var sut = CreateSut(httpContext);

        Assert.Throws<InvalidOperationException>(() => sut.UserId);
    }

    [Fact]
    public void UserId_Throws_WhenClaimValueExceedsTenCharacters()
    {
        // 11 chars — one over the Legacy ADDUSERID/CHANGEUSERID varchar(10) width. Must fail
        // loudly rather than silently truncate an identity into an audit column.
        var httpContext = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, "12345678901"));
        var sut = CreateSut(httpContext);

        Assert.Throws<InvalidOperationException>(() => sut.UserId);
    }

    [Fact]
    public void UserId_DoesNotThrow_WhenClaimValueIsExactlyTenCharacters()
    {
        var httpContext = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, "1234567890"));
        var sut = CreateSut(httpContext);

        Assert.Equal("1234567890", sut.UserId);
    }

    [Fact]
    public void VahedCode_ReturnsClaimValue_WhenPresent()
    {
        var httpContext = AuthenticatedContext(
            new Claim(ClaimTypes.NameIdentifier, "user1"),
            new Claim("vahed_code", "0001"));
        var sut = CreateSut(httpContext);

        Assert.Equal("0001", sut.VahedCode);
    }

    [Fact]
    public void VahedCode_ReturnsNull_WhenClaimAbsent()
    {
        var httpContext = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, "user1"));
        var sut = CreateSut(httpContext);

        Assert.Null(sut.VahedCode);
    }

    [Fact]
    public void VahedCode_ReturnsNull_WhenNoHttpContext()
    {
        var sut = CreateSut(httpContext: null);

        Assert.Null(sut.VahedCode);
    }

    [Fact]
    public void IsInRole_ReturnsTrue_WhenPrincipalHasRoleClaim()
    {
        var httpContext = AuthenticatedContext(
            new Claim(ClaimTypes.NameIdentifier, "user1"),
            new Claim(ClaimTypes.Role, "Accountant"));
        var sut = CreateSut(httpContext);

        Assert.True(sut.IsInRole("Accountant"));
    }

    [Fact]
    public void IsInRole_ReturnsFalse_WhenPrincipalLacksRoleClaim()
    {
        var httpContext = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, "user1"));
        var sut = CreateSut(httpContext);

        Assert.False(sut.IsInRole("Accountant"));
    }

    [Fact]
    public void IsInRole_ReturnsFalse_WhenNoHttpContext()
    {
        var sut = CreateSut(httpContext: null);

        Assert.False(sut.IsInRole("Accountant"));
    }
}
