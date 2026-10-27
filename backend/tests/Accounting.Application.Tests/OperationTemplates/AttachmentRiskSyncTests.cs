using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.OperationTemplates;
using Accounting.Domain.Entity;
using Accounting.Domain.OperationTemplates;
using Accounting.Domain.ValueObjects;
using Moq;
using static Accounting.Application.Tests.OperationTemplates.TemplateBuilders;

namespace Accounting.Application.Tests.OperationTemplates;

public class AttachmentRiskSyncTests
{
    private const string Other = "مبلغ بسیار بیشتر از معمول این عملیات در واحد است.";

    private static OperationExecution Exec(RiskLevel? level, params string[] reasons) => new()
    {
        Id = Guid.NewGuid(), VoucherId = Guid.NewGuid(), VahedCode = "0101", CreatedBy = "creator", TemplateCode = "T",
        InputJson = "{}", VoucherNo = "1", OperationTemplateId = Guid.NewGuid(), RiskLevel = level,
        RiskReasonsJson = RiskText.SerializeList(reasons),
        WarningsJson = reasons.Contains(OperationRiskEvaluator.AttachmentReason)
            ? RiskText.SerializeList([OperationRiskEvaluator.AttachmentWarning, "هشدار دیگر"]) : null,
    };

    [Fact]
    public void Upload_RemovesOnlyReason_DropsToLow()
    {
        var e = Exec(RiskLevel.Medium, OperationRiskEvaluator.AttachmentReason);
        Assert.True(AttachmentRiskSync.Apply(e, true, true));
        Assert.Equal(RiskLevel.Low, e.RiskLevel);
        Assert.Null(e.RiskReasonsJson);
        Assert.Equal(["هشدار دیگر"], RiskText.ParseList(e.WarningsJson));
    }

    [Fact]
    public void Upload_WithOtherMediumReason_StaysMedium()
    {
        var e = Exec(RiskLevel.Medium, OperationRiskEvaluator.AttachmentReason, Other);
        Assert.True(AttachmentRiskSync.Apply(e, true, true));
        Assert.Equal(RiskLevel.Medium, e.RiskLevel);
        Assert.Equal([Other], RiskText.ParseList(e.RiskReasonsJson));
    }

    [Fact]
    public void Upload_HighRisk_StaysHigh()
    {
        var e = Exec(RiskLevel.High, OperationRiskEvaluator.AttachmentReason);
        Assert.True(AttachmentRiskSync.Apply(e, true, true));
        Assert.Equal(RiskLevel.High, e.RiskLevel);
    }

    [Fact]
    public void Upload_NotFlagged_NoChange()
    {
        var e = Exec(RiskLevel.Low);
        Assert.False(AttachmentRiskSync.Apply(e, true, true));
    }

    [Fact]
    public void LegacyExecutionWithoutRisk_NoChange()
    {
        var e = Exec(null, OperationRiskEvaluator.AttachmentReason);
        Assert.False(AttachmentRiskSync.Apply(e, true, true));
        Assert.Null(e.RiskLevel);
    }

    [Fact]
    public void LastFileRemoved_Required_RaisesToMedium()
    {
        var e = Exec(RiskLevel.Low);
        Assert.True(AttachmentRiskSync.Apply(e, true, false));
        Assert.Equal(RiskLevel.Medium, e.RiskLevel);
        Assert.Contains(OperationRiskEvaluator.AttachmentReason, RiskText.ParseList(e.RiskReasonsJson));
        Assert.Contains(OperationRiskEvaluator.AttachmentWarning, RiskText.ParseList(e.WarningsJson));
    }

    [Fact]
    public void LastFileRemoved_NotRequired_NoChange()
    {
        Assert.False(AttachmentRiskSync.Apply(Exec(RiskLevel.Low), false, false));
    }

    // ─────────── در هندلرها ───────────

    private readonly Mock<IHesabyarAuditRepository> _audit = new();
    private readonly Mock<IVoucherAttachmentRepository> _attachments = new();
    private readonly Mock<IOperationTemplateRepository> _templates = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    private OperationExecution Setup(OperationExecution e, params VoucherAttachmentDto[] existing)
    {
        _user.Setup(u => u.UserId).Returns("creator");
        _audit.Setup(a => a.ListExecutionsByVouchersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([e]);
        _attachments.Setup(a => a.GetVoucherAsync(e.VoucherId, It.IsAny<CancellationToken>())).ReturnsAsync(("1405", DocLife.Temporary));
        _attachments.Setup(a => a.ListAsync(e.VoucherId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var t = Simple(); t.Id = e.OperationTemplateId!.Value; t.RequireAttachment = true;
        _templates.Setup(r => r.GetFullAsync(t.Id, It.IsAny<CancellationToken>())).ReturnsAsync(t);
        return e;
    }

    [Fact]
    public async Task UploadHandler_ClearsAttachmentRisk()
    {
        var e = Setup(Exec(RiskLevel.Medium, OperationRiskEvaluator.AttachmentReason));
        await new UploadVoucherAttachmentHandler(_audit.Object, _attachments.Object, _uow.Object, _user.Object)
            .Handle(new UploadVoucherAttachmentCommand(e.VoucherId, "f.pdf", null, [1]) { VahedCode = e.VahedCode }, default);
        Assert.Equal(RiskLevel.Low, e.RiskLevel);
        _audit.Verify(a => a.UpdateExecutionRiskAsync(e, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteHandler_LastFile_RestoresAttachmentRisk()
    {
        var file = new VoucherAttachmentDto(Guid.NewGuid(), "f.pdf", null, 1, null, null);
        var e = Setup(Exec(RiskLevel.Low), file);
        var row = new TB_ATTACH { ID = file.Id, VOUCHERSHEAD_ID = e.VoucherId, ATTACH_NAME = "f.pdf", VAHEDCODE = e.VahedCode, YEAR = "1405" };
        _attachments.Setup(a => a.FindAsync(e.VoucherId, row.ID, It.IsAny<CancellationToken>())).ReturnsAsync(row);

        await new DeleteVoucherAttachmentHandler(_audit.Object, _attachments.Object, _uow.Object, _user.Object, _templates.Object)
            .Handle(new DeleteVoucherAttachmentCommand(e.VoucherId, row.ID) { VahedCode = e.VahedCode }, default);

        Assert.Equal(RiskLevel.Medium, e.RiskLevel);
        _audit.Verify(a => a.UpdateExecutionRiskAsync(e, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteHandler_OtherFilesRemain_NoRiskChange()
    {
        var file = new VoucherAttachmentDto(Guid.NewGuid(), "f.pdf", null, 1, null, null);
        var other = new VoucherAttachmentDto(Guid.NewGuid(), "g.pdf", null, 1, null, null);
        var e = Setup(Exec(RiskLevel.Low), file, other);
        var row = new TB_ATTACH { ID = file.Id, VOUCHERSHEAD_ID = e.VoucherId, ATTACH_NAME = "f.pdf", VAHEDCODE = e.VahedCode, YEAR = "1405" };
        _attachments.Setup(a => a.FindAsync(e.VoucherId, row.ID, It.IsAny<CancellationToken>())).ReturnsAsync(row);

        await new DeleteVoucherAttachmentHandler(_audit.Object, _attachments.Object, _uow.Object, _user.Object, _templates.Object)
            .Handle(new DeleteVoucherAttachmentCommand(e.VoucherId, row.ID) { VahedCode = e.VahedCode }, default);

        Assert.Equal(RiskLevel.Low, e.RiskLevel);
        _audit.Verify(a => a.UpdateExecutionRiskAsync(It.IsAny<OperationExecution>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
