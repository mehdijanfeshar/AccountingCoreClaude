using System.Globalization;
using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.AccountCodes.GetTafsiliLevels;
using Accounting.Application.Queries.Tafsilis;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.Common;

/// <summary>قواعد ارسال درخواست پرداخت، حل تفصیلی بستانکاران و اعتبارسنجی تفصیلی‌های درخواست پرداخت.</summary>
public sealed class PaymentRequestRuleServicesTests
{
    private const string Vahed = "V1";
    private static readonly Guid Id = Guid.NewGuid();

    // ── PaymentRequestSubmitRuleChecker ─────────────────────────────────────

    private readonly Mock<ITreasurySettingReadRepository> _settings = new();
    private readonly Mock<IPaymentRequestRepository> _requests = new();

    private PaymentRequestSubmitRuleChecker Checker()
    {
        _settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(TreasuryTestData.Setting());
        return new PaymentRequestSubmitRuleChecker(_settings.Object, _requests.Object);
    }

    private static string TodayJalali()
    {
        var c = new PersianCalendar();
        var now = DateTime.Now;
        return $"{c.GetYear(now):0000}{c.GetMonth(now):00}{c.GetDayOfMonth(now):00}";
    }

    [Fact]
    public async Task Submit_SettingsMissing_Throws()
    {
        _settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((Accounting.Application.Queries.Treasury.TreasurySettingDto?)null);
        var sut = new PaymentRequestSubmitRuleChecker(_settings.Object, _requests.Object);

        await Assert.ThrowsAsync<PaymentRequestSettingsMissingException>(
            () => sut.EnsureSubmittableAsync(Id, null, null, false, "99991231", Vahed, null));
    }

    [Fact]
    public async Task Submit_DueDateInThePast_Throws()
        => await Assert.ThrowsAsync<PaymentRequestDueDatePastException>(
            () => Checker().EnsureSubmittableAsync(Id, null, null, false, "13000101", Vahed, null));

    [Fact]
    public async Task Submit_DueDateToday_Throws_MustBeStrictlyFuture()
        => await Assert.ThrowsAsync<PaymentRequestDueDatePastException>(
            () => Checker().EnsureSubmittableAsync(Id, null, null, false, TodayJalali(), Vahed, null));

    [Fact]
    public async Task Submit_InvoiceRefButNotApproved_Throws()
        => await Assert.ThrowsAsync<PaymentRequestInvoiceNotApprovedException>(
            () => Checker().EnsureSubmittableAsync(Id, null, "INV-1", false, "99991231", Vahed, null));

    [Fact]
    public async Task Submit_NoInvoiceRef_ApprovalFlagIrrelevant_Passes()
        => await Checker().EnsureSubmittableAsync(Id, null, null, false, "99991231", Vahed, null);

    [Fact]
    public async Task Submit_DuplicateInvoiceForBeneficiary_Throws()
    {
        var excluded = Guid.NewGuid();
        _requests.Setup(r => r.ExistsDuplicateAsync("123", "INV-1", Vahed, excluded, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<PaymentRequestDuplicateException>(
            () => Checker().EnsureSubmittableAsync(Id, "123", "INV-1", true, "99991231", Vahed, excluded));
    }

    [Fact]
    public async Task Submit_NoNationalId_SkipsDuplicateCheck()
    {
        await Checker().EnsureSubmittableAsync(Id, null, "INV-1", true, "99991231", Vahed, null);

        _requests.Verify(r => r.ExistsDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Submit_AllGood_Passes()
        => await Checker().EnsureSubmittableAsync(Id, "123", "INV-1", true, "99991231", Vahed, null);

    // ── PaymentRequestPayablesTafsiliResolver ───────────────────────────────

    private static PaymentRequestPayablesTafsiliResolver Resolver(params TafsiliLevelDto[] levels)
    {
        var lookup = new Mock<ITafsiliLookupReadRepository>();
        lookup.Setup(l => l.GetActiveLevelsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(levels);
        return new PaymentRequestPayablesTafsiliResolver(lookup.Object);
    }

    [Fact]
    public async Task Payables_NoLevels_ReturnsEmpty()
        => Assert.Empty(await Resolver().ResolvePayablesLinksAsync(Guid.NewGuid(), Id, null));

    [Fact]
    public async Task Payables_MultipleLevels_ThrowsAccountConfig()
        => await Assert.ThrowsAsync<PaymentRequestVoucherAccountConfigException>(
            () => Resolver(new TafsiliLevelDto(Guid.NewGuid(), 1, "a", true), new TafsiliLevelDto(Guid.NewGuid(), 2, "b", true))
                .ResolvePayablesLinksAsync(Guid.NewGuid(), Id, Guid.NewGuid()));

    [Fact]
    public async Task Payables_OneLevelButNoBeneficiary_Throws()
        => await Assert.ThrowsAsync<PaymentRequestPayablesBeneficiaryRequiredException>(
            () => Resolver(new TafsiliLevelDto(Guid.NewGuid(), 1, "a", true))
                .ResolvePayablesLinksAsync(Guid.NewGuid(), Id, null));

    [Fact]
    public async Task Payables_OneLevelWithBeneficiary_ReturnsSingleLink()
    {
        var level = Guid.NewGuid();
        var tafsili = Guid.NewGuid();

        var links = await Resolver(new TafsiliLevelDto(level, 1, "a", true)).ResolvePayablesLinksAsync(Guid.NewGuid(), Id, tafsili);

        var link = Assert.Single(links);
        Assert.Equal(tafsili, link.TafsiliId);
        Assert.Equal(level, link.LevelId);
    }

    [Fact]
    public async Task Payables_EnsureNoTafsiliRequired_AccountWithLevels_Throws()
        => await Assert.ThrowsAsync<PaymentRequestVoucherAccountConfigException>(
            () => Resolver(new TafsiliLevelDto(Guid.NewGuid(), 1, "a", true)).EnsureNoTafsiliRequiredAsync(Guid.NewGuid(), "بانک"));

    [Fact]
    public async Task Payables_EnsureNoTafsiliRequired_PlainAccount_Passes()
        => await Resolver().EnsureNoTafsiliRequiredAsync(Guid.NewGuid(), "بانک");

    // ── PaymentRequestTafsiliValidator ──────────────────────────────────────

    [Fact]
    public async Task CostCenter_TafsiliNotFound_ThrowsNotFound_BeforeGuard()
    {
        var guard = new Mock<IVoucherTafsiliLevelGuard>();
        var sut = new PaymentRequestTafsiliValidator(guard.Object, Mock.Of<ITafsiliReadRepository>(), Mock.Of<ITreasurySettingReadRepository>(), Mock.Of<ITreasuryBeneficiaryTafsiliReadRepository>());

        await Assert.ThrowsAsync<NotFoundException>(() => sut.EnsureCostCenterTafsilisValidAsync(
            Guid.NewGuid(), new[] { new PaymentRequestTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()) }, Vahed));

        guard.Verify(g => g.EnsureSatisfiedAsync(It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CostCenter_NoLinks_StillAsksGuardToVerifyRequiredLevels()
    {
        var guard = new Mock<IVoucherTafsiliLevelGuard>();
        var account = Guid.NewGuid();
        var sut = new PaymentRequestTafsiliValidator(guard.Object, Mock.Of<ITafsiliReadRepository>(), Mock.Of<ITreasurySettingReadRepository>(), Mock.Of<ITreasuryBeneficiaryTafsiliReadRepository>());

        await sut.EnsureCostCenterTafsilisValidAsync(account, Array.Empty<PaymentRequestTafsiliLinkInput>(), Vahed);

        guard.Verify(g => g.EnsureSatisfiedAsync(account, It.Is<IReadOnlyCollection<VoucherDetailTafsiliLinkInput>>(l => l.Count == 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Beneficiary_Null_Skips()
    {
        var settings = new Mock<ITreasurySettingReadRepository>();
        var sut = new PaymentRequestTafsiliValidator(Mock.Of<IVoucherTafsiliLevelGuard>(), Mock.Of<ITafsiliReadRepository>(), settings.Object, Mock.Of<ITreasuryBeneficiaryTafsiliReadRepository>());

        await sut.EnsureBeneficiaryTafsiliValidAsync(null, Vahed);

        settings.Verify(s => s.GetByVahedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Beneficiary_GroupNotConfigured_Throws()
    {
        var settings = new Mock<ITreasurySettingReadRepository>();
        settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(TreasuryTestData.Setting());
        var sut = new PaymentRequestTafsiliValidator(Mock.Of<IVoucherTafsiliLevelGuard>(), Mock.Of<ITafsiliReadRepository>(), settings.Object, Mock.Of<ITreasuryBeneficiaryTafsiliReadRepository>());

        await Assert.ThrowsAsync<PaymentRequestBeneficiaryGroupNotConfiguredException>(() => sut.EnsureBeneficiaryTafsiliValidAsync(Guid.NewGuid(), Vahed));
    }

    [Fact]
    public async Task Beneficiary_NotInGroup_Throws_InGroup_Passes()
    {
        var group = Guid.NewGuid();
        var tafsili = Guid.NewGuid();
        var settings = new Mock<ITreasurySettingReadRepository>();
        settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(TreasuryTestData.Setting(beneficiaryGroupId: group));
        var members = new Mock<ITreasuryBeneficiaryTafsiliReadRepository>();
        members.Setup(m => m.IsMemberOfGroupAsync(tafsili, group, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = new PaymentRequestTafsiliValidator(Mock.Of<IVoucherTafsiliLevelGuard>(), Mock.Of<ITafsiliReadRepository>(), settings.Object, members.Object);

        await Assert.ThrowsAsync<PaymentRequestBeneficiaryTafsiliNotInGroupException>(() => sut.EnsureBeneficiaryTafsiliValidAsync(tafsili, Vahed));

        members.Setup(m => m.IsMemberOfGroupAsync(tafsili, group, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await sut.EnsureBeneficiaryTafsiliValidAsync(tafsili, Vahed);
    }
}
