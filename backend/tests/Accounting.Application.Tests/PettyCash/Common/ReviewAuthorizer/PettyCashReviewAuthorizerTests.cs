using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Common.ReviewAuthorizer;

public class PettyCashReviewAuthorizerTests
{
    private readonly Mock<IPettyCashFundReviewerRepository> _reviewers = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Guid _docId = Guid.NewGuid();
    private readonly Guid _fundId = Guid.NewGuid();

    public PettyCashReviewAuthorizerTests() => _user.SetupGet(u => u.UserId).Returns("u1");

    private PettyCashReviewAuthorizer Sut() => new(_reviewers.Object, _user.Object);

    private void Roles(params PettyCashRole[] roles)
        => _reviewers.Setup(r => r.GetActiveRolesAsync(_fundId, "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles.ToList());

    [Fact]
    public async Task Handle_MatchingRoleAndNotCreator_ReturnsMatchingRoles()
    {
        Roles(PettyCashRole.Inspector, PettyCashRole.Treasurer);

        var result = await Sut().EnsureCanReviewAsync(_docId, _fundId, "creator",
            new[] { PettyCashRole.Inspector }, default);

        Assert.Equal(new[] { PettyCashRole.Inspector }, result);
    }

    [Fact]
    public async Task Handle_NoActiveRoles_ThrowsAccessDenied()
    {
        Roles();

        await Assert.ThrowsAsync<PettyCashReviewerAccessDeniedException>(() =>
            Sut().EnsureCanReviewAsync(_docId, _fundId, "creator", new[] { PettyCashRole.Inspector }, default));
    }

    [Fact]
    public async Task Handle_RoleNotAllowedForAction_ThrowsAccessDenied()
    {
        Roles(PettyCashRole.Inspector);

        await Assert.ThrowsAsync<PettyCashReviewerAccessDeniedException>(() =>
            Sut().EnsureCanReviewAsync(_docId, _fundId, "creator", new[] { PettyCashRole.FinanceManager }, default));
    }

    [Fact]
    public async Task Handle_CallerIsCreator_ThrowsSelfReviewConflict()
    {
        Roles(PettyCashRole.Inspector);

        await Assert.ThrowsAsync<PettyCashSelfReviewConflictException>(() =>
            Sut().EnsureCanReviewAsync(_docId, _fundId, "u1", new[] { PettyCashRole.Inspector }, default));
    }

    [Fact]
    public async Task Handle_CreatorWithoutRole_ThrowsAccessDeniedBeforeSelfReview()
    {
        // ترتیب: ۴۰۳ پیش از ۴۰۹
        Roles();

        await Assert.ThrowsAsync<PettyCashReviewerAccessDeniedException>(() =>
            Sut().EnsureCanReviewAsync(_docId, _fundId, "u1", new[] { PettyCashRole.Inspector }, default));
    }
}
