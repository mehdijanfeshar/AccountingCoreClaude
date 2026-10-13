using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Common.Authorizers;

public class PettyCashReplenishmentAuthorizerTests
{
    private readonly Mock<IPettyCashFundReviewerRepository> _reviewers = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Guid _fund = Guid.NewGuid();

    private PettyCashReplenishmentAuthorizer Sut()
    {
        _user.SetupGet(u => u.UserId).Returns("u1");
        return new(_reviewers.Object, _user.Object);
    }

    [Fact]
    public async Task Handle_HasAllowedRole_ReturnsIntersection()
    {
        _reviewers.Setup(r => r.GetActiveRolesAsync(_fund, "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PettyCashRole> { PettyCashRole.Treasurer, PettyCashRole.Inspector });

        var res = await Sut().EnsureHasRoleAsync(_fund, new[] { PettyCashRole.Treasurer });

        Assert.Equal(new[] { PettyCashRole.Treasurer }, res);
    }

    [Fact]
    public async Task Handle_NoMatchingRole_ThrowsRoleRequired()
    {
        _reviewers.Setup(r => r.GetActiveRolesAsync(_fund, "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PettyCashRole> { PettyCashRole.Inspector });

        await Assert.ThrowsAsync<PettyCashReplenishmentRoleRequiredException>(
            () => Sut().EnsureHasRoleAsync(_fund, new[] { PettyCashRole.Treasurer }));
    }

    [Fact]
    public async Task Handle_NoRolesAtAll_ThrowsRoleRequired()
    {
        _reviewers.Setup(r => r.GetActiveRolesAsync(_fund, "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PettyCashRole>());

        await Assert.ThrowsAsync<PettyCashReplenishmentRoleRequiredException>(
            () => Sut().EnsureHasRoleAsync(_fund, new[] { PettyCashRole.Treasurer }));
    }
}
