using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.OperationTemplates;
using Accounting.Domain.Entity;
using Accounting.Domain.OperationTemplates;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.OperationTemplates;

public class VoucherAttachmentHandlersTests
{
    private static readonly Guid VoucherId = Guid.NewGuid();
    private const string Vahed = "0101";

    private readonly Mock<IHesabyarAuditRepository> _audit = new();
    private readonly Mock<IVoucherAttachmentRepository> _attachments = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public VoucherAttachmentHandlersTests()
    {
        _user.Setup(u => u.UserId).Returns("creator");
        _audit.Setup(a => a.ListExecutionsByVouchersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new OperationExecution
            {
                Id = Guid.NewGuid(), VoucherId = VoucherId, VahedCode = Vahed, CreatedBy = "creator",
                TemplateCode = "T", InputJson = "{}", VoucherNo = "1",
            }]);
        _attachments.Setup(a => a.GetVoucherAsync(VoucherId, It.IsAny<CancellationToken>())).ReturnsAsync(("1405", DocLife.Temporary));
        _attachments.Setup(a => a.ListAsync(VoucherId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private void ActAs(string userId, params string[] roles)
    {
        _user.Setup(u => u.UserId).Returns(userId);
        _user.Setup(u => u.IsInRole(It.IsAny<string>())).Returns<string>(r => roles.Contains(r));
    }

    private UploadVoucherAttachmentHandler Upload() => new(_audit.Object, _attachments.Object, _uow.Object, _user.Object);
    private DeleteVoucherAttachmentHandler Delete() => new(_audit.Object, _attachments.Object, _uow.Object, _user.Object);
    private ListVoucherAttachmentsHandler List() => new(_audit.Object, _attachments.Object, _user.Object);
    private GetVoucherAttachmentFileHandler Download() => new(_audit.Object, _attachments.Object, _user.Object);

    private static UploadVoucherAttachmentCommand UploadCmd(string vahed = Vahed) =>
        new(VoucherId, "factor.pdf", " فاکتور ", [1, 2, 3]) { VahedCode = vahed };

    private static ListVoucherAttachmentsQuery ListQuery(string vahed = Vahed) => new(VoucherId) { VahedCode = vahed };

    // ─────────── دسترسی ───────────

    [Fact]
    public async Task OtherUnitsVoucher_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Upload().Handle(UploadCmd("9999"), default));
        await Assert.ThrowsAsync<NotFoundException>(() => List().Handle(ListQuery("9999"), default));
    }

    [Fact]
    public async Task VoucherWithoutHesabyarExecution_IsNotFound()
    {
        _audit.Setup(a => a.ListExecutionsByVouchersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        await Assert.ThrowsAsync<NotFoundException>(() => Upload().Handle(UploadCmd(), default));
    }

    [Fact]
    public async Task List_ByCreator_Allowed()
    {
        Assert.Empty(await List().Handle(ListQuery(), default));
    }

    [Fact]
    public async Task List_ByReviewerWhoIsNotCreator_Allowed()
    {
        ActAs("accountant", AppRoles.MaliAdmin);
        Assert.Empty(await List().Handle(ListQuery(), default));
    }

    [Fact]
    public async Task List_ByStranger_Denied()
    {
        ActAs("stranger");
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() => List().Handle(ListQuery(), default));
    }

    [Fact]
    public async Task Upload_ByReviewerWhoIsNotCreator_Denied()
    {
        ActAs("accountant", AppRoles.MaliAdmin);
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() => Upload().Handle(UploadCmd(), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─────────── وضعیت سند ───────────

    [Theory]
    [InlineData(DocLife.Reviewed)]
    [InlineData(DocLife.Accepted)]
    public async Task Upload_AfterReview_IsConflict(DocLife life)
    {
        _attachments.Setup(a => a.GetVoucherAsync(VoucherId, It.IsAny<CancellationToken>())).ReturnsAsync(("1405", life));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Upload().Handle(UploadCmd(), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Upload_OnReturnedDraft_Allowed()
    {
        _attachments.Setup(a => a.GetVoucherAsync(VoucherId, It.IsAny<CancellationToken>())).ReturnsAsync(("1405", DocLife.Draft));
        await Upload().Handle(UploadCmd(), default);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_DeletedVoucher_IsNotFound()
    {
        _attachments.Setup(a => a.GetVoucherAsync(VoucherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string?, DocLife?)?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => Upload().Handle(UploadCmd(), default));
    }

    [Fact]
    public async Task Upload_AtFileLimit_IsConflict()
    {
        var full = Enumerable.Range(0, VoucherAttachmentAccessLimits.MaxFilesPerVoucher)
            .Select(i => new VoucherAttachmentDto(Guid.NewGuid(), $"f{i}", null, 1, null, null)).ToList();
        _attachments.Setup(a => a.ListAsync(VoucherId, It.IsAny<CancellationToken>())).ReturnsAsync(full);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Upload().Handle(UploadCmd(), default));
    }

    // ─────────── ذخیره ───────────

    [Fact]
    public async Task Upload_Valid_StagesLegacyRowWithNextRadif()
    {
        TB_ATTACH? added = null;
        _attachments.Setup(a => a.GetMaxRadifAsync(Vahed, "1405", It.IsAny<CancellationToken>())).ReturnsAsync(41);
        _attachments.Setup(a => a.AddAsync(It.IsAny<TB_ATTACH>(), It.IsAny<CancellationToken>()))
            .Callback<TB_ATTACH, CancellationToken>((a, _) => added = a);

        var id = await Upload().Handle(UploadCmd(), default);

        Assert.NotNull(added);
        Assert.Equal(id, added!.ID);
        Assert.Equal(VoucherId, added.VOUCHERSHEAD_ID);
        Assert.Equal(42, added.ATTACH_RADIF);
        Assert.Equal("factor.pdf", added.ATTACH_NAME);
        Assert.Equal("فاکتور", added.TITLE);
        Assert.Equal(3, added.ATTACH_SIZE);
        Assert.Equal(Vahed, added.VAHEDCODE);
        Assert.Equal("1405", added.YEAR);
        Assert.Equal("creator", added.ADDUSERID);
        Assert.False(added.ISDELETED);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_SoftDeletes()
    {
        var row = new TB_ATTACH { ID = Guid.NewGuid(), VOUCHERSHEAD_ID = VoucherId, ATTACH_NAME = "a", VAHEDCODE = Vahed, YEAR = "1405" };
        _attachments.Setup(a => a.FindAsync(VoucherId, row.ID, It.IsAny<CancellationToken>())).ReturnsAsync(row);

        await Delete().Handle(new DeleteVoucherAttachmentCommand(VoucherId, row.ID) { VahedCode = Vahed }, default);

        Assert.True(row.ISDELETED);
        Assert.Equal("creator", row.CHANGEUSERID);
        Assert.NotNull(row.UPDATEDDATE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Missing_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Delete().Handle(new DeleteVoucherAttachmentCommand(VoucherId, Guid.NewGuid()) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Download_ReturnsNameAndBytes()
    {
        var row = new TB_ATTACH { ID = Guid.NewGuid(), ATTACH_NAME = "r.png", ATTACH_FILE = [9, 8], VAHEDCODE = Vahed, YEAR = "1405" };
        _attachments.Setup(a => a.FindAsync(VoucherId, row.ID, It.IsAny<CancellationToken>())).ReturnsAsync(row);

        var file = await Download().Handle(new GetVoucherAttachmentFileQuery(VoucherId, row.ID) { VahedCode = Vahed }, default);

        Assert.Equal("r.png", file.FileName);
        Assert.Equal(new byte[] { 9, 8 }, file.Content);
    }

    [Fact]
    public async Task Download_WithoutContent_IsNotFound()
    {
        var row = new TB_ATTACH { ID = Guid.NewGuid(), ATTACH_NAME = "r.png", VAHEDCODE = Vahed, YEAR = "1405" };
        _attachments.Setup(a => a.FindAsync(VoucherId, row.ID, It.IsAny<CancellationToken>())).ReturnsAsync(row);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Download().Handle(new GetVoucherAttachmentFileQuery(VoucherId, row.ID) { VahedCode = Vahed }, default));
    }

    // ─────────── اعتبارسنجی ───────────

    [Fact]
    public void Validator_RejectsEmptyOversizedAndNamelessFiles()
    {
        var v = new UploadVoucherAttachmentValidator();
        Assert.False(v.Validate(new UploadVoucherAttachmentCommand(VoucherId, "a.pdf", null, [])).IsValid);
        Assert.False(v.Validate(new UploadVoucherAttachmentCommand(VoucherId, "a.pdf", null, new byte[VoucherAttachmentAccessLimits.MaxFileBytes + 1])).IsValid);
        Assert.False(v.Validate(new UploadVoucherAttachmentCommand(VoucherId, "", null, [1])).IsValid);
        Assert.True(v.Validate(new UploadVoucherAttachmentCommand(VoucherId, "a.pdf", null, [1])).IsValid);
    }
}
