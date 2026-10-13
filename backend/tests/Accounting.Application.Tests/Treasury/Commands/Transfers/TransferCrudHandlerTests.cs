using Accounting.Application.Commands.Treasury.CreateTransfer;
using Accounting.Application.Commands.Treasury.DeleteTransfer;
using Accounting.Application.Commands.Treasury.SubmitTransfer;
using Accounting.Application.Commands.Treasury.UpdateTransfer;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.BankAccounts;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.Transfers;

/// <summary>ایجاد/ویرایش/حذف/ارسال انتقال وجه: ماشین حالت پیش‌نویس↔برگشتی→در انتظار خزانه‌دار و ثبت واحد.</summary>
public sealed class TransferCrudHandlerTests
{
    private const string Vahed = "V1";
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid Src = Guid.NewGuid();
    private static readonly Guid Dst = Guid.NewGuid();

    private readonly Mock<ITreasuryTransferRepository> _transfers = new();
    private readonly Mock<ITreasuryTransferEventRepository> _events = new();
    private readonly Mock<IBankAccountReadRepository> _banks = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IClientInfoProvider> _client = new();

    public TransferCrudHandlerTests()
    {
        _user.SetupGet(u => u.UserId).Returns("creator");
        _client.SetupGet(c => c.ClientIp).Returns("1.2.3.4");
        _banks.Setup(b => b.GetByIdAsync(It.IsAny<Guid>(), Vahed, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, string _, CancellationToken _) => Bank(id));
    }

    private static BankAccountDto Bank(Guid id) => new(
        id, "1", "h", null, null, null, null, null, null, Guid.NewGuid(), Vahed, null, null, null, null, null, false,
        new List<BankAccountTafsiliLinkDto>());

    private TB_TR_TRANSFER Existing(TransferState state)
    {
        var t = new TB_TR_TRANSFER { ID = Id, STATE = state, YEAR = "1405", VAHEDCODE = Vahed, ADDUSERID = "creator" };
        _transfers.Setup(r => r.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        return t;
    }

    // ── Create ──────────────────────────────────────────────────────────────

    private CreateTransferCommand CreateCmd() => new(Src, Dst, 500m, "14050101", TreasuryPaymentMethod.Satna, "علت", "1405") { VahedCode = Vahed };

    [Fact]
    public async Task Create_Valid_StagesDraftWithGeneratedCodeAndUnit()
    {
        _transfers.Setup(t => t.GetNextCodeAsync(Vahed, "1405", It.IsAny<CancellationToken>())).ReturnsAsync(7);
        TB_TR_TRANSFER? added = null;
        _transfers.Setup(t => t.AddAsync(It.IsAny<TB_TR_TRANSFER>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_TRANSFER, CancellationToken>((t, _) => added = t).Returns(Task.CompletedTask);
        var sut = new CreateTransferCommandHandler(_transfers.Object, _banks.Object, _uow.Object, _user.Object);

        var id = await sut.Handle(CreateCmd(), default);

        Assert.NotNull(added);
        Assert.Equal(id, added!.ID);
        Assert.Equal("TRF-000007", added.CODE);
        Assert.Equal(TransferState.Draft, added.STATE);
        Assert.Equal(Vahed, added.VAHEDCODE);
        Assert.Equal("creator", added.ADDUSERID);
        Assert.Equal(500m, added.AMOUNT);
        Assert.False(added.ISDELETED);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_SourceBankMissing_ThrowsNotFoundAndDoesNotSave()
    {
        _banks.Setup(b => b.GetByIdAsync(Src, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);
        var sut = new CreateTransferCommandHandler(_transfers.Object, _banks.Object, _uow.Object, _user.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(CreateCmd(), default));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_DestBankMissing_ThrowsNotFound()
    {
        _banks.Setup(b => b.GetByIdAsync(Dst, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);
        var sut = new CreateTransferCommandHandler(_transfers.Object, _banks.Object, _uow.Object, _user.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(CreateCmd(), default));
    }

    // ── Update ──────────────────────────────────────────────────────────────

    private UpdateTransferCommand UpdateCmd() => new(Id, Src, Dst, 900m, "14050202", TreasuryPaymentMethod.Satna, "جدید") { VahedCode = Vahed };

    [Theory]
    [InlineData(TransferState.Draft)]
    [InlineData(TransferState.Returned)]
    public async Task Update_EditableState_AppliesFields(TransferState state)
    {
        var t = Existing(state);
        var sut = new UpdateTransferCommandHandler(_transfers.Object, _banks.Object, _uow.Object, _user.Object);

        await sut.Handle(UpdateCmd(), default);

        Assert.Equal(900m, t.AMOUNT);
        Assert.Equal("14050202", t.TRANSFER_DATE);
        Assert.Equal("جدید", t.REASON);
        Assert.Equal("creator", t.CHANGEUSERID);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(TransferState.PendingTreasurer)]
    [InlineData(TransferState.Executed)]
    [InlineData(TransferState.Rejected)]
    public async Task Update_NonEditableState_ThrowsStateConflict(TransferState state)
    {
        Existing(state);
        var sut = new UpdateTransferCommandHandler(_transfers.Object, _banks.Object, _uow.Object, _user.Object);

        await Assert.ThrowsAsync<TreasuryTransferStateConflictException>(() => sut.Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_NotFoundOrDeleted_Throws()
    {
        var sut = new UpdateTransferCommandHandler(_transfers.Object, _banks.Object, _uow.Object, _user.Object);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(UpdateCmd(), default));

        Existing(TransferState.Draft).ISDELETED = true;
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_DestBankMissing_ThrowsNotFound_AndEntityUntouched()
    {
        var t = Existing(TransferState.Draft);
        t.AMOUNT = 1m;
        _banks.Setup(b => b.GetByIdAsync(Dst, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);
        var sut = new UpdateTransferCommandHandler(_transfers.Object, _banks.Object, _uow.Object, _user.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(UpdateCmd(), default));

        Assert.Equal(1m, t.AMOUNT);
    }

    // ── Delete ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TransferState.Draft)]
    [InlineData(TransferState.Returned)]
    public async Task Delete_EditableState_SoftDeletes(TransferState state)
    {
        var t = Existing(state);
        var sut = new DeleteTransferCommandHandler(_transfers.Object, _uow.Object, _user.Object);

        await sut.Handle(new DeleteTransferCommand(Id) { VahedCode = Vahed }, default);

        Assert.True(t.ISDELETED);
        Assert.Equal("creator", t.CHANGEUSERID);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(TransferState.PendingTreasurer)]
    [InlineData(TransferState.Executed)]
    public async Task Delete_NonEditableState_Throws(TransferState state)
    {
        var t = Existing(state);
        var sut = new DeleteTransferCommandHandler(_transfers.Object, _uow.Object, _user.Object);

        await Assert.ThrowsAsync<TreasuryTransferStateConflictException>(
            () => sut.Handle(new DeleteTransferCommand(Id) { VahedCode = Vahed }, default));

        Assert.False(t.ISDELETED);
    }

    [Fact]
    public async Task Delete_NotFound_Throws()
    {
        var sut = new DeleteTransferCommandHandler(_transfers.Object, _uow.Object, _user.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new DeleteTransferCommand(Id) { VahedCode = Vahed }, default));
    }

    // ── Submit ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TransferState.Draft)]
    [InlineData(TransferState.Returned)]
    public async Task Submit_EditableState_MovesToPendingTreasurerAndLogsEvent(TransferState from)
    {
        var t = Existing(from);
        TB_TR_TRANSFER_EVENT? ev = null;
        _events.Setup(e => e.AddAsync(It.IsAny<TB_TR_TRANSFER_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_TRANSFER_EVENT, CancellationToken>((e, _) => ev = e).Returns(Task.CompletedTask);
        var sut = new SubmitTransferCommandHandler(_transfers.Object, _events.Object, _uow.Object, _user.Object, _client.Object);

        await sut.Handle(new SubmitTransferCommand(Id) { VahedCode = Vahed }, default);

        Assert.Equal(TransferState.PendingTreasurer, t.STATE);
        Assert.NotNull(ev);
        Assert.Equal(TransferEventAction.Submit, ev!.ACTION);
        Assert.Equal(from, ev.FROM_STATE);
        Assert.Equal(TransferState.PendingTreasurer, ev.TO_STATE);
        Assert.Equal("1.2.3.4", ev.CLIENT_IP);
        Assert.Equal(Vahed, ev.VAHEDCODE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(TransferState.PendingTreasurer)]
    [InlineData(TransferState.Executed)]
    [InlineData(TransferState.Rejected)]
    public async Task Submit_OtherStates_ThrowsAndLogsNothing(TransferState state)
    {
        Existing(state);
        var sut = new SubmitTransferCommandHandler(_transfers.Object, _events.Object, _uow.Object, _user.Object, _client.Object);

        await Assert.ThrowsAsync<TreasuryTransferStateConflictException>(
            () => sut.Handle(new SubmitTransferCommand(Id) { VahedCode = Vahed }, default));

        _events.Verify(e => e.AddAsync(It.IsAny<TB_TR_TRANSFER_EVENT>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Submit_NotFound_Throws()
    {
        var sut = new SubmitTransferCommandHandler(_transfers.Object, _events.Object, _uow.Object, _user.Object, _client.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.Handle(new SubmitTransferCommand(Id) { VahedCode = Vahed }, default));
    }

    // ── Validators ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateValidator_Valid_Passes()
        => Assert.True(new CreateTransferCommandValidator().Validate(CreateCmd()).IsValid);

    [Fact]
    public void CreateValidator_SameSourceAndDest_Fails()
        => Assert.False(new CreateTransferCommandValidator().Validate(CreateCmd() with { DestBankAccountId = Src }).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateValidator_NonPositiveAmount_Fails(int amount)
        => Assert.False(new CreateTransferCommandValidator().Validate(CreateCmd() with { Amount = amount }).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("1405-01-01")]
    [InlineData("1405010")]
    public void CreateValidator_BadDate_Fails(string date)
        => Assert.False(new CreateTransferCommandValidator().Validate(CreateCmd() with { TransferDate = date }).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("14a5")]
    [InlineData("14055")]
    public void CreateValidator_BadYear_Fails(string year)
        => Assert.False(new CreateTransferCommandValidator().Validate(CreateCmd() with { Year = year }).IsValid);

    [Fact]
    public void CreateValidator_EmptyReason_InvalidMethod_Fail()
    {
        var v = new CreateTransferCommandValidator();
        Assert.False(v.Validate(CreateCmd() with { Reason = "" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { TransferMethod = (TreasuryPaymentMethod)999 }).IsValid);
    }

    [Fact]
    public void CreateValidator_VahedCodeTooLongOrEmpty_Fails()
    {
        var v = new CreateTransferCommandValidator();
        Assert.False(v.Validate(CreateCmd() with { VahedCode = "" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { VahedCode = "12345" }).IsValid);
    }

    [Fact]
    public void UpdateValidator_Valid_Passes_EmptyId_SameBank_Fail()
    {
        var v = new UpdateTransferCommandValidator();
        Assert.True(v.Validate(UpdateCmd()).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { Id = Guid.Empty }).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { DestBankAccountId = Src }).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { Amount = 0m }).IsValid);
    }
}
