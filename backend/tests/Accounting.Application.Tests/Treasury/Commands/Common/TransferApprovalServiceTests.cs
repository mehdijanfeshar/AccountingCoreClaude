using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.BankAccounts;
using Accounting.Application.Queries.Treasury;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.Common;

/// <summary>تأیید انتقال: نقش خزانه‌دار، SoD، موجودی مبدأ، سقف روزانه، صدور سند و گذار وضعیت.</summary>
public sealed class TransferApprovalServiceTests
{
    private const string Vahed = "V1";
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid SourceId = Guid.NewGuid();
    private static readonly Guid AccountCodeId = Guid.NewGuid();

    private readonly Mock<ITreasuryTransferRepository> _transfers = new();
    private readonly Mock<ITreasuryTransferEventRepository> _events = new();
    private readonly Mock<ITreasuryTreasurerAuthorizer> _treasurer = new();
    private readonly Mock<ITreasurySettingReadRepository> _settings = new();
    private readonly Mock<IBankAccountReadRepository> _banks = new();
    private readonly Mock<ITreasuryBankAccountBalanceReadRepository> _balance = new();
    private readonly Mock<ITransferVoucherBuilder> _voucher = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IClientInfoProvider> _client = new();
    private readonly List<TB_TR_TRANSFER_EVENT> _added = new();

    public TransferApprovalServiceTests()
    {
        _user.SetupGet(u => u.UserId).Returns("treasurer1");
        _events.Setup(e => e.AddAsync(It.IsAny<TB_TR_TRANSFER_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_TRANSFER_EVENT, CancellationToken>((e, _) => _added.Add(e))
            .Returns(Task.CompletedTask);
        _banks.Setup(b => b.GetByIdAsync(SourceId, Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BankAccount(AccountCodeId));
        _balance.Setup(b => b.GetBalanceAsync(AccountCodeId, It.IsAny<IReadOnlyCollection<Guid>>(), Vahed, "1405", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10_000m);
        _settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Setting(dailyLimit: 5_000m));
        _transfers.Setup(t => t.GetExecutedAmountForSourceOnDateAsync(SourceId, "14050101", Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _voucher.Setup(v => v.BuildAndStageAsync(It.IsAny<TB_TR_TRANSFER>(), Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoucherResult(Guid.NewGuid()));
    }

    private static BankAccountDto BankAccount(Guid? accountCodeId) => new(
        SourceId, "123", "holder", null, null, null, null, null, null, accountCodeId, Vahed, null, null, null, null, null, false,
        new List<BankAccountTafsiliLinkDto> { new(Guid.NewGuid(), Guid.NewGuid()) });

    private static TreasurySettingDto Setting(decimal? dailyLimit)
        => TreasuryTestData.Setting(dailyLimit: dailyLimit);

    private static PaymentRequestVoucherBuildResult VoucherResult(Guid id)
        => new(id, "55", "14050101", "1405", Array.Empty<PaymentRequestVoucherLineResult>());

    private TransferApprovalService Sut() => new(
        _transfers.Object, _events.Object, _treasurer.Object, _settings.Object, _banks.Object,
        _balance.Object, _voucher.Object, _user.Object, _client.Object);

    private TB_TR_TRANSFER Transfer(TransferState state = TransferState.PendingTreasurer, decimal amount = 1_000m, string creator = "creator")
    {
        var t = new TB_TR_TRANSFER
        {
            ID = Id, STATE = state, AMOUNT = amount, ADDUSERID = creator, SOURCE_BANK_ACCOUNT_ID = SourceId,
            TRANSFER_DATE = "14050101", YEAR = "1405", VAHEDCODE = Vahed,
        };
        _transfers.Setup(r => r.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        return t;
    }

    [Fact]
    public async Task Approve_NotFound_Throws()
    {
        _transfers.Setup(r => r.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((TB_TR_TRANSFER?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Fact]
    public async Task Approve_Deleted_ThrowsNotFound()
    {
        Transfer().ISDELETED = true;

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Theory]
    [InlineData(TransferState.Draft)]
    [InlineData(TransferState.Executed)]
    [InlineData(TransferState.Returned)]
    [InlineData(TransferState.Rejected)]
    public async Task Approve_NotPendingTreasurer_ThrowsStateConflict(TransferState state)
    {
        Transfer(state);

        await Assert.ThrowsAsync<TreasuryTransferStateConflictException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Fact]
    public async Task Approve_NotTreasurer_PropagatesAuthorizerException()
    {
        Transfer();
        _treasurer.Setup(t => t.EnsureTreasurerAsync(Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryTreasurerRoleRequiredException());

        await Assert.ThrowsAsync<TreasuryTreasurerRoleRequiredException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Fact]
    public async Task Approve_CreatorIsApprover_Throws()
    {
        Transfer(creator: "treasurer1");

        await Assert.ThrowsAsync<TreasuryTransferApproverConflictException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Fact]
    public async Task Approve_SourceBankAccountMissing_ThrowsNotFound()
    {
        Transfer();
        _banks.Setup(b => b.GetByIdAsync(SourceId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Fact]
    public async Task Approve_SourceBankAccountWithoutAccountCode_ThrowsNotFound()
    {
        Transfer();
        _banks.Setup(b => b.GetByIdAsync(SourceId, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(BankAccount(null));

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Fact]
    public async Task Approve_InsufficientBalance_Throws()
    {
        Transfer(amount: 10_001m);

        await Assert.ThrowsAsync<TreasuryTransferInsufficientBalanceException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Fact]
    public async Task Approve_DailyLimitNotDefined_Throws()
    {
        Transfer();
        _settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(Setting(dailyLimit: null));

        await Assert.ThrowsAsync<TreasurySettingValueMissingException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Fact]
    public async Task Approve_SettingsRowMissing_Throws()
    {
        Transfer();
        _settings.Setup(s => s.GetByVahedAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((TreasurySettingDto?)null);

        await Assert.ThrowsAsync<TreasurySettingValueMissingException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Fact]
    public async Task Approve_DailyLimitExceededWithAlreadyTransferred_Throws()
    {
        Transfer(amount: 1_000m);
        _transfers.Setup(t => t.GetExecutedAmountForSourceOnDateAsync(SourceId, "14050101", Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4_001m);

        await Assert.ThrowsAsync<TreasuryTransferDailyLimitExceededException>(() => Sut().ApproveAsync(Id, Vahed, "R"));
    }

    [Fact]
    public async Task Approve_ExactlyAtDailyLimit_Succeeds()
    {
        var t = Transfer(amount: 1_000m);
        _transfers.Setup(r => r.GetExecutedAmountForSourceOnDateAsync(SourceId, "14050101", Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4_000m);

        await Sut().ApproveAsync(Id, Vahed, "R");

        Assert.Equal(TransferState.Executed, t.STATE);
    }

    [Fact]
    public async Task Approve_Valid_IssuesVoucherAndExecutes()
    {
        var t = Transfer();
        var voucherId = Guid.NewGuid();
        _voucher.Setup(v => v.BuildAndStageAsync(t, Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoucherResult(voucherId));

        await Sut().ApproveAsync(Id, Vahed, "BANKREF");

        Assert.Equal(TransferState.Executed, t.STATE);
        Assert.Equal(voucherId, t.VOUCHER_ID);
        Assert.Equal("BANKREF", t.BANK_REFERENCE);
        Assert.Equal("treasurer1", t.APPROVED_BY);
        Assert.NotNull(t.APPROVED_DATE);
        var ev = Assert.Single(_added);
        Assert.Equal(TransferEventAction.Approve, ev.ACTION);
        Assert.Equal(TransferState.Executed, ev.TO_STATE);
        Assert.Contains("BANKREF", ev.NOTE);
    }

    [Fact]
    public async Task Approve_ChecksFailBeforeVoucherIsBuilt()
    {
        Transfer(amount: 99_999m);

        await Assert.ThrowsAsync<TreasuryTransferInsufficientBalanceException>(() => Sut().ApproveAsync(Id, Vahed, "R"));

        _voucher.Verify(v => v.BuildAndStageAsync(It.IsAny<TB_TR_TRANSFER>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Return_Valid_SetsReturnedWithReason()
    {
        var t = Transfer();

        await Sut().ReturnAsync(Id, Vahed, "اصلاح");

        Assert.Equal(TransferState.Returned, t.STATE);
        Assert.Equal("اصلاح", t.RETURN_REASON);
        Assert.Equal(TransferEventAction.Return, Assert.Single(_added).ACTION);
    }

    [Fact]
    public async Task Return_Creator_Throws()
    {
        Transfer(creator: "treasurer1");

        await Assert.ThrowsAsync<TreasuryTransferApproverConflictException>(() => Sut().ReturnAsync(Id, Vahed, "r"));
    }

    [Fact]
    public async Task Reject_Valid_SetsRejected()
    {
        var t = Transfer();

        await Sut().RejectAsync(Id, Vahed, "رد");

        Assert.Equal(TransferState.Rejected, t.STATE);
        Assert.Equal(TransferEventAction.Reject, Assert.Single(_added).ACTION);
    }

    [Fact]
    public async Task Reject_NotPending_Throws()
    {
        Transfer(TransferState.Executed);

        await Assert.ThrowsAsync<TreasuryTransferStateConflictException>(() => Sut().RejectAsync(Id, Vahed, "r"));
    }

    [Fact]
    public async Task Reject_NotTreasurer_Throws()
    {
        Transfer();
        _treasurer.Setup(t => t.EnsureTreasurerAsync(Vahed, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryTreasurerRoleRequiredException());

        await Assert.ThrowsAsync<TreasuryTreasurerRoleRequiredException>(() => Sut().RejectAsync(Id, Vahed, "r"));
    }
}
