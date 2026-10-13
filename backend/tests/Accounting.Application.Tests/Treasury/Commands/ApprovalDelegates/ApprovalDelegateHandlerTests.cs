using Accounting.Application.Commands.Treasury.ApprovePaymentRequest;
using Accounting.Application.Commands.Treasury.ApproveTransfer;
using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Commands.Treasury.RejectPaymentRequest;
using Accounting.Application.Commands.Treasury.RejectTransfer;
using Accounting.Application.Commands.Treasury.ReturnPaymentRequest;
using Accounting.Application.Commands.Treasury.ReturnTransfer;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.ApprovalDelegates;

/// <summary>
/// handlerهای نازک تأیید/برگشت/رد (درخواست پرداخت و انتقال) فقط به سرویس تأیید واگذار می‌کنند و ذخیره می‌کنند؛
/// قواعد خودِ سرویس‌ها در تست‌های سرویس پوشش داده می‌شود.
/// </summary>
public sealed class ApprovalDelegateHandlerTests
{
    private static readonly Guid Id = Guid.NewGuid();

    [Fact]
    public async Task ApprovePaymentRequest_DelegatesAsNonBulkAndSaves()
    {
        var svc = new Mock<IPaymentRequestApprovalService>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new ApprovePaymentRequestCommandHandler(svc.Object, uow.Object);

        await sut.Handle(new ApprovePaymentRequestCommand(Id, "ok") { VahedCode = "V1" }, default);

        svc.Verify(s => s.ApproveAsync(Id, "V1", "ok", false, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApprovePaymentRequest_ServiceThrows_DoesNotSave()
    {
        var svc = new Mock<IPaymentRequestApprovalService>();
        svc.Setup(s => s.ApproveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        var uow = new Mock<IUnitOfWork>();
        var sut = new ApprovePaymentRequestCommandHandler(svc.Object, uow.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(new ApprovePaymentRequestCommand(Id, null) { VahedCode = "V1" }, default));

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReturnPaymentRequest_DelegatesAndSaves()
    {
        var svc = new Mock<IPaymentRequestApprovalService>();
        var uow = new Mock<IUnitOfWork>();

        await new ReturnPaymentRequestCommandHandler(svc.Object, uow.Object)
            .Handle(new ReturnPaymentRequestCommand(Id, "اصلاح") { VahedCode = "V1" }, default);

        svc.Verify(s => s.ReturnAsync(Id, "V1", "اصلاح", It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectPaymentRequest_DelegatesAndSaves()
    {
        var svc = new Mock<IPaymentRequestApprovalService>();
        var uow = new Mock<IUnitOfWork>();

        await new RejectPaymentRequestCommandHandler(svc.Object, uow.Object)
            .Handle(new RejectPaymentRequestCommand(Id, "رد") { VahedCode = "V1" }, default);

        svc.Verify(s => s.RejectAsync(Id, "V1", "رد", It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReturnTransfer_DelegatesAndSaves()
    {
        var svc = new Mock<ITransferApprovalService>();
        var uow = new Mock<IUnitOfWork>();

        await new ReturnTransferCommandHandler(svc.Object, uow.Object)
            .Handle(new ReturnTransferCommand(Id, "r") { VahedCode = "V1" }, default);

        svc.Verify(s => s.ReturnAsync(Id, "V1", "r", It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectTransfer_DelegatesAndSaves()
    {
        var svc = new Mock<ITransferApprovalService>();
        var uow = new Mock<IUnitOfWork>();

        await new RejectTransferCommandHandler(svc.Object, uow.Object)
            .Handle(new RejectTransferCommand(Id, "r") { VahedCode = "V1" }, default);

        svc.Verify(s => s.RejectAsync(Id, "V1", "r", It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveTransfer_Success_CommitsInTransaction()
    {
        var svc = new Mock<ITransferApprovalService>();
        var uow = new Mock<IUnitOfWork>();

        await new ApproveTransferCommandHandler(svc.Object, uow.Object)
            .Handle(new ApproveTransferCommand(Id, "REF1") { VahedCode = "V1" }, default);

        svc.Verify(s => s.ApproveAsync(Id, "V1", "REF1", It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveTransfer_ServiceThrows_RollsBackAndRethrows()
    {
        var svc = new Mock<ITransferApprovalService>();
        svc.Setup(s => s.ApproveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        var uow = new Mock<IUnitOfWork>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ApproveTransferCommandHandler(svc.Object, uow.Object)
                .Handle(new ApproveTransferCommand(Id, "REF1") { VahedCode = "V1" }, default));

        uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Validators ──────────────────────────────────────────────────────────

    [Fact]
    public void ReasonValidators_EmptyReason_Fail_ValidReason_Pass()
    {
        Assert.False(new ReturnPaymentRequestCommandValidator().Validate(new ReturnPaymentRequestCommand(Id, "")).IsValid);
        Assert.False(new RejectPaymentRequestCommandValidator().Validate(new RejectPaymentRequestCommand(Id, " ")).IsValid);
        Assert.False(new ReturnTransferCommandValidator().Validate(new ReturnTransferCommand(Id, "")).IsValid);
        Assert.False(new RejectTransferCommandValidator().Validate(new RejectTransferCommand(Id, "")).IsValid);

        Assert.True(new ReturnPaymentRequestCommandValidator().Validate(new ReturnPaymentRequestCommand(Id, "x")).IsValid);
        Assert.True(new RejectPaymentRequestCommandValidator().Validate(new RejectPaymentRequestCommand(Id, "x")).IsValid);
        Assert.True(new ReturnTransferCommandValidator().Validate(new ReturnTransferCommand(Id, "x")).IsValid);
        Assert.True(new RejectTransferCommandValidator().Validate(new RejectTransferCommand(Id, "x")).IsValid);
    }

    [Fact]
    public void ReasonValidators_ReasonOver1000_Fails()
    {
        var longText = new string('a', 1001);
        Assert.False(new ReturnPaymentRequestCommandValidator().Validate(new ReturnPaymentRequestCommand(Id, longText)).IsValid);
        Assert.False(new RejectTransferCommandValidator().Validate(new RejectTransferCommand(Id, longText)).IsValid);
    }

    [Fact]
    public void ApproveValidators_EmptyId_Fails()
    {
        Assert.False(new ApprovePaymentRequestCommandValidator().Validate(new ApprovePaymentRequestCommand(Guid.Empty, null)).IsValid);
        Assert.False(new ApproveTransferCommandValidator().Validate(new ApproveTransferCommand(Guid.Empty, "R")).IsValid);
    }

    [Fact]
    public void ApprovePaymentRequestValidator_NoteTooLong_Fails()
        => Assert.False(new ApprovePaymentRequestCommandValidator().Validate(new ApprovePaymentRequestCommand(Id, new string('a', 1001))).IsValid);

    [Fact]
    public void ApproveTransferValidator_BankReferenceRules()
    {
        var v = new ApproveTransferCommandValidator();
        Assert.False(v.Validate(new ApproveTransferCommand(Id, "")).IsValid);
        Assert.False(v.Validate(new ApproveTransferCommand(Id, new string('a', 101))).IsValid);
        Assert.True(v.Validate(new ApproveTransferCommand(Id, new string('a', 100))).IsValid);
    }
}
