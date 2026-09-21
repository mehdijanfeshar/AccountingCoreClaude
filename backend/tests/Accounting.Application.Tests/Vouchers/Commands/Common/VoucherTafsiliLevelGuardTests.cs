using Accounting.Application.AccountCodes.Queries.GetTafsiliLevels;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Vouchers.Commands.Common;
using Moq;

namespace Accounting.Application.Tests.Vouchers.Commands.Common;

/// <summary>
/// The «تفصیلی الزامی» rule itself — open as risk #12 since the project began, because the rich
/// model that carried it was deliberately dropped and the Legacy mechanism replacing it was
/// understood but never rebuilt.
/// </summary>
public sealed class VoucherTafsiliLevelGuardTests
{
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CostCentreLevel = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectLevel = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UnconfiguredLevel = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private static Mock<ITafsiliLookupReadRepository> Lookup(params TafsiliLevelDto[] levels)
    {
        var lookup = new Mock<ITafsiliLookupReadRepository>();
        lookup
            .Setup(r => r.GetActiveLevelsAsync(AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(levels);

        return lookup;
    }

    private static TafsiliLevelDto Level(Guid id, int code, string name) => new(id, code, name, true);

    private static VoucherDetailTafsiliLinkInput Link(Guid levelId) => new(Guid.NewGuid(), levelId);

    [Fact]
    public async Task Passes_WhenEveryConfiguredLevelHasAValue()
    {
        var guard = new VoucherTafsiliLevelGuard(
            Lookup(Level(CostCentreLevel, 1, "مرکز هزینه"), Level(ProjectLevel, 3, "پروژه")).Object);

        await guard.EnsureSatisfiedAsync(AccountId, [Link(CostCentreLevel), Link(ProjectLevel)]);
    }

    [Fact]
    public async Task Passes_WhenTheAccountRequiresNothingAndNoneWasSent()
    {
        var guard = new VoucherTafsiliLevelGuard(Lookup().Object);

        await guard.EnsureSatisfiedAsync(AccountId, []);
    }

    /// <summary>Rule A — the half an accountant filling the form actually hits.</summary>
    [Fact]
    public async Task Rejects_WhenAConfiguredLevelHasNoValue()
    {
        var guard = new VoucherTafsiliLevelGuard(
            Lookup(Level(CostCentreLevel, 1, "مرکز هزینه"), Level(ProjectLevel, 3, "پروژه")).Object);

        var exception = await Assert.ThrowsAsync<RequiredTafsiliLevelMissingException>(
            () => guard.EnsureSatisfiedAsync(AccountId, [Link(CostCentreLevel)]));

        // The names are the point: a caller told only "something is missing" cannot act on it.
        Assert.Equal(["پروژه"], exception.LevelNames);
        Assert.Contains("پروژه", exception.PublicDetail);
    }

    [Fact]
    public async Task Rejects_WhenNoTafsiliAtAllWasSentForAnAccountThatRequiresIt()
    {
        var guard = new VoucherTafsiliLevelGuard(Lookup(Level(CostCentreLevel, 1, "مرکز هزینه")).Object);

        var exception = await Assert.ThrowsAsync<RequiredTafsiliLevelMissingException>(
            () => guard.EnsureSatisfiedAsync(AccountId, []));

        Assert.Equal(AccountId, exception.AccountCodeId);
    }

    /// <summary>
    /// Rule B — the half that is easy to leave out, because an extra value feels harmless. It is
    /// not: TB_VOUCHERDETAIL_LINK_TAFSILI.LEVEL_ID has no FK, so an unconfigured level would be
    /// stored happily and then disagree with the معین's configuration in every report.
    /// </summary>
    [Fact]
    public async Task Rejects_WhenAValueIsSentForAnUnconfiguredLevel()
    {
        var guard = new VoucherTafsiliLevelGuard(Lookup(Level(CostCentreLevel, 1, "مرکز هزینه")).Object);

        var exception = await Assert.ThrowsAsync<TafsiliLevelNotPermittedException>(
            () => guard.EnsureSatisfiedAsync(AccountId, [Link(CostCentreLevel), Link(UnconfiguredLevel)]));

        Assert.Equal(UnconfiguredLevel, exception.LevelId);

        // The level id is not echoed back: a caller sending one either has stale configuration or
        // is not using the form, and neither is helped by seeing the id again.
        Assert.DoesNotContain(UnconfiguredLevel.ToString(), exception.PublicDetail);
    }

    [Fact]
    public async Task Rejects_WhenTafsiliIsSentForALineWithNoAccount()
    {
        var guard = new VoucherTafsiliLevelGuard(Lookup().Object);

        await Assert.ThrowsAsync<TafsiliLevelNotPermittedException>(
            () => guard.EnsureSatisfiedAsync(null, [Link(CostCentreLevel)]));
    }

    [Fact]
    public async Task Passes_ForALineWithNeitherAccountNorTafsili()
    {
        var guard = new VoucherTafsiliLevelGuard(Lookup().Object);

        await guard.EnsureSatisfiedAsync(null, []);
    }

    /// <summary>
    /// When both halves are violated, the missing-required error wins — it is the one the user can
    /// act on, and reporting the other first would send them looking for a problem they do not have.
    /// </summary>
    [Fact]
    public async Task ReportsTheMissingRequiredLevel_WhenBothHalvesAreViolatedAtOnce()
    {
        var guard = new VoucherTafsiliLevelGuard(Lookup(Level(CostCentreLevel, 1, "مرکز هزینه")).Object);

        await Assert.ThrowsAsync<RequiredTafsiliLevelMissingException>(
            () => guard.EnsureSatisfiedAsync(AccountId, [Link(UnconfiguredLevel)]));
    }

    /// <summary>
    /// The composite create path checks every line, and a real voucher posts many lines against the
    /// same handful of معین. Without the memo that is one database read per line.
    /// </summary>
    [Fact]
    public async Task ReadsTheLevelsOncePerAccount_HoweverManyLinesAreChecked()
    {
        var lookup = Lookup(Level(CostCentreLevel, 1, "مرکز هزینه"));
        var guard = new VoucherTafsiliLevelGuard(lookup.Object);

        for (var line = 0; line < 5; line++)
        {
            await guard.EnsureSatisfiedAsync(AccountId, [Link(CostCentreLevel)]);
        }

        lookup.Verify(r => r.GetActiveLevelsAsync(AccountId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var lookup = Lookup();
        var guard = new VoucherTafsiliLevelGuard(lookup.Object);

        await guard.EnsureSatisfiedAsync(AccountId, [], token);

        lookup.Verify(r => r.GetActiveLevelsAsync(AccountId, token), Times.Once);
    }
}
