using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.Common;

/// <summary>تست‌های کمکی‌های خالص و Authorizerهای خزانه‌داری.</summary>
public sealed class TreasuryHelpersTests
{
    // ── PaymentRequestStageRoleMap ──────────────────────────────────────────

    [Theory]
    [InlineData(PaymentRequestState.PendingUnitManager, TreasuryRole.UnitManager)]
    [InlineData(PaymentRequestState.PendingFinanceManager, TreasuryRole.FinanceManager)]
    [InlineData(PaymentRequestState.PendingCeo, TreasuryRole.Ceo)]
    public void StageRoleMap_PendingState_ReturnsMatchingRole(PaymentRequestState state, TreasuryRole expected)
        => Assert.Equal(expected, PaymentRequestStageRoleMap.RequiredRoleForState(state));

    [Theory]
    [InlineData(PaymentRequestState.Draft)]
    [InlineData(PaymentRequestState.ReadyForExecution)]
    [InlineData(PaymentRequestState.Returned)]
    [InlineData(PaymentRequestState.Rejected)]
    [InlineData(PaymentRequestState.Executed)]
    [InlineData(PaymentRequestState.Suspended)]
    public void StageRoleMap_NonPendingState_ReturnsNull(PaymentRequestState state)
        => Assert.Null(PaymentRequestStageRoleMap.RequiredRoleForState(state));

    // ── PaymentRequestAmountCalculator ──────────────────────────────────────

    [Fact]
    public void Calculator_PercentsSupplied_DerivesAmountsAndIgnoresInputAmounts()
    {
        var (vat, ins, net) = PaymentRequestAmountCalculator.Compute(1_000_000m, 10m, 999m, 5m, 888m);

        Assert.Equal(100_000m, vat);
        Assert.Equal(50_000m, ins);
        Assert.Equal(1_050_000m, net);
    }

    [Fact]
    public void Calculator_NoPercents_UsesInputAmounts()
    {
        var (vat, ins, net) = PaymentRequestAmountCalculator.Compute(1_000m, null, 90m, null, 40m);

        Assert.Equal(90m, vat);
        Assert.Equal(40m, ins);
        Assert.Equal(1_050m, net);
    }

    [Fact]
    public void Calculator_NothingSupplied_NetEqualsBase()
    {
        var (vat, ins, net) = PaymentRequestAmountCalculator.Compute(500m, null, null, null, null);

        Assert.Equal(0m, vat);
        Assert.Equal(0m, ins);
        Assert.Equal(500m, net);
    }

    [Fact]
    public void Calculator_RoundsHalfAwayFromZero()
    {
        // 9% از 25 = 2.25 → 2 ؛ 10% از 25 = 2.5 → 3
        Assert.Equal(2m, PaymentRequestAmountCalculator.Compute(25m, 9m, null, null, null).VatAmount);
        Assert.Equal(3m, PaymentRequestAmountCalculator.Compute(25m, 10m, null, null, null).VatAmount);
    }

    // ── BankStatementEditability ────────────────────────────────────────────

    [Fact]
    public void Editability_EnsureOpen_OpenState_DoesNotThrow()
        => BankStatementEditability.EnsureOpen(Guid.NewGuid(), BankStatementState.Open);

    [Fact]
    public void Editability_EnsureOpen_ClosedState_Throws()
        => Assert.Throws<TreasuryBankStatementStateConflictException>(
            () => BankStatementEditability.EnsureOpen(Guid.NewGuid(), BankStatementState.Closed));

    [Fact]
    public void Editability_EnsureClosed_ClosedState_DoesNotThrow()
        => BankStatementEditability.EnsureClosed(Guid.NewGuid(), BankStatementState.Closed);

    [Fact]
    public void Editability_EnsureClosed_OpenState_Throws()
        => Assert.Throws<TreasuryBankStatementStateConflictException>(
            () => BankStatementEditability.EnsureClosed(Guid.NewGuid(), BankStatementState.Open));

    // ── Authorizers ─────────────────────────────────────────────────────────

    private static (Mock<ITreasuryRoleRepository> Roles, ICurrentUser User) RolesFor(params TreasuryRole[] roles)
    {
        var repo = new Mock<ITreasuryRoleRepository>();
        repo.Setup(r => r.GetActiveRolesAsync("V1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        var user = new Mock<ICurrentUser>();
        user.SetupGet(u => u.UserId).Returns("u1");
        return (repo, user.Object);
    }

    [Fact]
    public async Task RoleAuthorizer_HasAllowedRole_ReturnsOnlyMatchedRoles()
    {
        var (repo, user) = RolesFor(TreasuryRole.FinanceManager, TreasuryRole.Treasurer);
        var sut = new TreasuryRoleAuthorizer(repo.Object, user);

        var matched = await sut.EnsureHasRoleAsync("V1", new[] { TreasuryRole.FinanceManager, TreasuryRole.Ceo });

        Assert.Equal(new[] { TreasuryRole.FinanceManager }, matched);
    }

    [Fact]
    public async Task RoleAuthorizer_NoAllowedRole_ThrowsTreasuryRoleRequired()
    {
        var (repo, user) = RolesFor(TreasuryRole.Treasurer);
        var sut = new TreasuryRoleAuthorizer(repo.Object, user);

        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(
            () => sut.EnsureHasRoleAsync("V1", new[] { TreasuryRole.FinanceManager }));
    }

    [Fact]
    public async Task TreasurerAuthorizer_IsTreasurer_Passes()
    {
        var (repo, user) = RolesFor(TreasuryRole.Treasurer);
        await new TreasuryTreasurerAuthorizer(repo.Object, user).EnsureTreasurerAsync("V1");
    }

    [Fact]
    public async Task TreasurerAuthorizer_NotTreasurer_Throws()
    {
        var (repo, user) = RolesFor(TreasuryRole.FinanceManager);
        await Assert.ThrowsAsync<TreasuryTreasurerRoleRequiredException>(
            () => new TreasuryTreasurerAuthorizer(repo.Object, user).EnsureTreasurerAsync("V1"));
    }

    [Fact]
    public async Task ExecutionAuthorizer_NotTreasurer_Throws()
    {
        var (repo, user) = RolesFor();
        await Assert.ThrowsAsync<PaymentRequestTreasurerRoleRequiredException>(
            () => new PaymentRequestExecutionAuthorizer(repo.Object, user).EnsureTreasurerAsync(Guid.NewGuid(), "V1"));
    }

    [Fact]
    public async Task ExecutionAuthorizer_IsTreasurer_Passes()
    {
        var (repo, user) = RolesFor(TreasuryRole.Treasurer);
        await new PaymentRequestExecutionAuthorizer(repo.Object, user).EnsureTreasurerAsync(Guid.NewGuid(), "V1");
    }
}
