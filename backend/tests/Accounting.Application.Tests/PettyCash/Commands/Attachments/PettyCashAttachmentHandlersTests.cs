using Accounting.Application.Commands.PettyCash.DeletePettyCashAttachment;
using Accounting.Application.Commands.PettyCash.UploadPettyCashAttachment;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.PettyCash;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.Attachments;

public class PettyCashAttachmentHandlersTests
{
    private readonly Mock<IPettyCashExpenseDocRepository> _docs = new();
    private readonly Mock<IPettyCashAttachmentRepository> _atts = new();
    private readonly Mock<IPettyCashDocEventReadRepository> _events = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Guid _docId = Guid.NewGuid();

    public PettyCashAttachmentHandlersTests() => _user.SetupGet(u => u.UserId).Returns("owner");

    private UploadPettyCashAttachmentCommandHandler Upload() => new(_docs.Object, _atts.Object, _events.Object, _uow.Object, _user.Object);
    private DeletePettyCashAttachmentCommandHandler Delete() => new(_docs.Object, _atts.Object, _events.Object, _uow.Object, _user.Object);

    private TB_PC_EXPENSE_DOC SetupDoc(PettyCashDocState state = PettyCashDocState.Draft, string owner = "owner", bool deleted = false)
    {
        var doc = new TB_PC_EXPENSE_DOC { ID = _docId, DOC_STATE = state, ADDUSERID = owner, YEAR = "1404", ISDELETED = deleted };
        _docs.Setup(r => r.GetForUpdateAsync(_docId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(doc);
        return doc;
    }

    private void SetupLastReturn(string? reasons)
        => _events.Setup(e => e.GetLastReturnEventAsync(_docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashDocEventDto(Guid.NewGuid(), PettyCashDocAction.Return, PettyCashDocState.PendingReview,
                PettyCashDocState.Returned, null, reasons, "insp", DateTime.UtcNow, null));

    private UploadPettyCashAttachmentCommand UpCmd(int size = 10) =>
        new(_docId, "a.pdf", "application/pdf", new byte[size]) { VahedCode = "0101" };

    // ---------- Upload ----------

    [Fact]
    public async Task Upload_DocMissing_ThrowsNotFound()
    {
        _docs.Setup(r => r.GetForUpdateAsync(_docId, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TB_PC_EXPENSE_DOC?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Upload().Handle(UpCmd(), default));
    }

    [Fact]
    public async Task Upload_DocDeleted_ThrowsNotFound()
    {
        SetupDoc(deleted: true);

        await Assert.ThrowsAsync<NotFoundException>(() => Upload().Handle(UpCmd(), default));
    }

    [Theory]
    [InlineData(PettyCashDocState.New)]
    [InlineData(PettyCashDocState.PendingReview)]
    [InlineData(PettyCashDocState.Approved)]
    public async Task Upload_NonEditableState_Throws(PettyCashDocState state)
    {
        SetupDoc(state);

        await Assert.ThrowsAsync<PettyCashDocNotEditableException>(() => Upload().Handle(UpCmd(), default));
    }

    [Fact]
    public async Task Upload_NotOwner_Throws()
    {
        SetupDoc(owner: "someone-else");

        await Assert.ThrowsAsync<PettyCashAttachmentOwnerOnlyException>(() => Upload().Handle(UpCmd(), default));
    }

    [Theory]
    [InlineData("2")] // فقط حساب هزینه
    [InlineData("3,4")]
    [InlineData(null)]
    public async Task Upload_ReturnedWithoutAttachmentReason_ThrowsFieldLocked(string? reasons)
    {
        SetupDoc(PettyCashDocState.Returned);
        SetupLastReturn(reasons);

        await Assert.ThrowsAsync<PettyCashReturnFieldLockedException>(() => Upload().Handle(UpCmd(), default));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2,1")]
    public async Task Upload_ReturnedWithAttachmentReason_Succeeds(string reasons)
    {
        SetupDoc(PettyCashDocState.Returned);
        SetupLastReturn(reasons);
        _atts.Setup(a => a.GetMaxRadifAsync(_docId, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var id = await Upload().Handle(UpCmd(), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Upload_TooLarge_ThrowsTooLarge()
    {
        SetupDoc();

        await Assert.ThrowsAsync<PettyCashAttachmentTooLargeException>(
            () => Upload().Handle(UpCmd(UploadPettyCashAttachmentCommandValidator.MaxAttachmentSizeBytes + 1), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Upload_ExactlyMaxSize_Succeeds()
    {
        SetupDoc();
        _atts.Setup(a => a.GetMaxRadifAsync(_docId, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var id = await Upload().Handle(UpCmd(UploadPettyCashAttachmentCommandValidator.MaxAttachmentSizeBytes), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Upload_Valid_StampsRadifVahedYearAndSaves()
    {
        SetupDoc();
        _atts.Setup(a => a.GetMaxRadifAsync(_docId, It.IsAny<CancellationToken>())).ReturnsAsync(4);
        TB_PC_ATTACHMENT? saved = null;
        _atts.Setup(a => a.AddAsync(It.IsAny<TB_PC_ATTACHMENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_ATTACHMENT, CancellationToken>((x, _) => saved = x).Returns(Task.CompletedTask);

        var id = await Upload().Handle(UpCmd(25), default);

        Assert.Equal(id, saved!.ID);
        Assert.Equal(5, saved.ATTACH_RADIF);
        Assert.Equal(25, saved.ATTACH_SIZE);
        Assert.Equal("0101", saved.VAHEDCODE);
        Assert.Equal("1404", saved.YEAR);
        Assert.Equal("owner", saved.ADDUSERID);
        Assert.False(saved.ISDELETED);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Upload validator ----------

    private readonly UploadPettyCashAttachmentCommandValidator _upVal = new();

    [Fact]
    public void UploadValidator_ValidCommand_Passes()
        => _upVal.TestValidate(UpCmd()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void UploadValidator_OversizedContent_Fails()
        => _upVal.TestValidate(UpCmd(UploadPettyCashAttachmentCommandValidator.MaxAttachmentSizeBytes + 1))
            .ShouldHaveValidationErrorFor(x => x.Content);

    [Fact]
    public void UploadValidator_EmptyContent_Fails()
        => _upVal.TestValidate(UpCmd(0)).ShouldHaveValidationErrorFor(x => x.Content);

    [Fact]
    public void UploadValidator_EmptyNameOrDocId_Fails()
    {
        var r = _upVal.TestValidate(new UploadPettyCashAttachmentCommand(Guid.Empty, "", null, new byte[1]));
        r.ShouldHaveValidationErrorFor(x => x.ExpenseDocId);
        r.ShouldHaveValidationErrorFor(x => x.AttachName);
    }

    [Fact]
    public void UploadValidator_NameTooLong_Fails()
        => _upVal.TestValidate(new UploadPettyCashAttachmentCommand(_docId, new string('a', 256), null, new byte[1]))
            .ShouldHaveValidationErrorFor(x => x.AttachName);

    // ---------- Delete ----------

    private DeletePettyCashAttachmentCommand DelCmd(Guid attId) => new(_docId, attId) { VahedCode = "0101" };

    [Fact]
    public async Task Delete_DocMissing_ThrowsNotFound()
    {
        _docs.Setup(r => r.GetForUpdateAsync(_docId, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TB_PC_EXPENSE_DOC?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Delete().Handle(DelCmd(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Delete_NonEditableState_Throws()
    {
        SetupDoc(PettyCashDocState.Approved);

        await Assert.ThrowsAsync<PettyCashDocNotEditableException>(() => Delete().Handle(DelCmd(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Delete_NotOwner_Throws()
    {
        SetupDoc(owner: "x");

        await Assert.ThrowsAsync<PettyCashAttachmentOwnerOnlyException>(() => Delete().Handle(DelCmd(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Delete_ReturnedWithoutAttachmentReason_ThrowsFieldLocked()
    {
        SetupDoc(PettyCashDocState.Returned);
        SetupLastReturn("3");

        await Assert.ThrowsAsync<PettyCashReturnFieldLockedException>(() => Delete().Handle(DelCmd(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Delete_AttachmentMissing_ThrowsNotFound()
    {
        SetupDoc();
        var attId = Guid.NewGuid();
        _atts.Setup(a => a.GetForUpdateAsync(attId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((TB_PC_ATTACHMENT?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Delete().Handle(DelCmd(attId), default));
    }

    [Fact]
    public async Task Delete_AttachmentBelongsToOtherDoc_ThrowsNotFound()
    {
        SetupDoc();
        var attId = Guid.NewGuid();
        _atts.Setup(a => a.GetForUpdateAsync(attId, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_PC_ATTACHMENT { ID = attId, EXPENSE_DOC_ID = Guid.NewGuid() });

        await Assert.ThrowsAsync<NotFoundException>(() => Delete().Handle(DelCmd(attId), default));
    }

    [Fact]
    public async Task Delete_AlreadyDeleted_IsIdempotentAndDoesNotSave()
    {
        SetupDoc();
        var attId = Guid.NewGuid();
        _atts.Setup(a => a.GetForUpdateAsync(attId, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_PC_ATTACHMENT { ID = attId, EXPENSE_DOC_ID = _docId, ISDELETED = true });

        await Delete().Handle(DelCmd(attId), default);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_Valid_SoftDeletesAndSaves()
    {
        SetupDoc();
        var attId = Guid.NewGuid();
        var att = new TB_PC_ATTACHMENT { ID = attId, EXPENSE_DOC_ID = _docId };
        _atts.Setup(a => a.GetForUpdateAsync(attId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(att);

        await Delete().Handle(DelCmd(attId), default);

        Assert.True(att.ISDELETED);
        Assert.Equal("owner", att.CHANGEUSERID);
        Assert.NotNull(att.UPDATEDDATE);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
