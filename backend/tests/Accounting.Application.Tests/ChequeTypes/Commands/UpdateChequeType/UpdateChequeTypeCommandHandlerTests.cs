using Accounting.Application.ChequeTypes.Commands.UpdateChequeType;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.ChequeTypes.Commands.UpdateChequeType;

public sealed class UpdateChequeTypeCommandHandlerTests
{
    private static UpdateChequeTypeCommand ValidCommand(Guid id) => new(
        Id: id,
        ChequeTypeTitle: "Updated",
        ChequeWidth: 210,
        ChequeHeight: 95,
        ChequeImage: new byte[] { 9, 9, 9 },
        ChequeAdateFont: "Tahoma-10",
        ChequeAdateLeft: 15,
        ChequeAdateTop: 16,
        ChequeAdateWidth: 17,
        ChequeNdateFont: "Tahoma-11",
        ChequeNdateLeft: 25,
        ChequeNdateTop: 26,
        ChequeNdateWidth: 27,
        ChequeAamountFont: "Tahoma-12",
        ChequeAamountLeft: 35,
        ChequeAamountTop: 36,
        ChequeAamountWidth: 37,
        ChequeLamountFont: "Tahoma-13",
        ChequeLamountLeft: 45,
        ChequeLamountTop: 46,
        ChequeLamountWidth: 47,
        ChequeNamountFont: "Tahoma-14",
        ChequeNamountLeft: 55,
        ChequeNamountTop: 56,
        ChequeNamountWidth: 57,
        ChequeDescribe1Font: "Tahoma-15",
        ChequeDescribe1Left: 65,
        ChequeDescribe1Top: 66,
        ChequeDescribe1Width: 67,
        ChequeDescribe2Font: "Tahoma-16",
        ChequeDescribe2Left: 75,
        ChequeDescribe2Top: 76,
        ChequeDescribe2Width: 77,
        ChequeBreaklineFont: "Tahoma-17",
        ChequeBreaklineLeft: 85,
        ChequeBreaklineTop: 86,
        ChequeBreaklineWidth: 87,
        PrinterMargineTop: 7,
        PrinterMargineLeft: 8,
        PrinterType: "Epson",
        Year: "1404",
        VahedCode: "0200");

    private static TB_CHECK_TYPE ExistingEntity(Guid id, bool isDeleted = false) => new()
    {
        ID = id,
        CHEQUE_TYPE_TITLE = "old",
        YEAR = "1403",
        VAHEDCODE = "0100",
        ADDUSERID = "creator1",
        CREATEDDATE = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CHANGEUSERID = null,
        UPDATEDDATE = null,
        ISDELETED = isDeleted,
    };

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "editor1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_ExistingNonDeletedRecord_WritesRepresentativeSpreadOfWritableFieldsFromCommand()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IChequeTypeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand(id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.ChequeTypeTitle, entity.CHEQUE_TYPE_TITLE);
        Assert.Equal(command.ChequeWidth, entity.CHEQUE_WIDTH);
        Assert.Equal(command.ChequeHeight, entity.CHEQUE_HEIGHT);
        Assert.Equal(command.ChequeImage, entity.CHEQUE_IMAGE);
        Assert.Equal(command.ChequeAdateFont, entity.CHEQUE_ADATE_FONT);
        Assert.Equal(command.ChequeAdateLeft, entity.CHEQUE_ADATE_LEFT);
        Assert.Equal(command.ChequeNamountWidth, entity.CHEQUE_NAMOUNT_WIDTH);
        Assert.Equal(command.ChequeDescribe1Font, entity.CHEQUE_DESCRIBE1_FONT);
        Assert.Equal(command.ChequeDescribe2Width, entity.CHEQUE_DESCRIBE2_WIDTH);
        Assert.Equal(command.ChequeBreaklineFont, entity.CHEQUE_BREAKLINE_FONT);
        Assert.Equal(command.PrinterMargineTop, entity.PRINTER_MARGINE_TOP);
        Assert.Equal(command.PrinterMargineLeft, entity.PRINTER_MARGINE_LEFT);
        Assert.Equal(command.PrinterType, entity.PRINTER_TYPE);
        Assert.Equal(command.Year, entity.YEAR);
        Assert.Equal(command.VahedCode, entity.VAHEDCODE);
    }

    [Fact]
    public async Task Handle_SetsChangeUserIdFromCurrentUser_AndStampsUpdatedDate()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IChequeTypeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr02");

        var handler = new UpdateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal("srvusr02", entity.CHANGEUSERID);
        Assert.NotNull(entity.UPDATEDDATE);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_NeverMutatesIdentityOrCreationAuditOrIsDeleted()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var originalId = entity.ID;
        var originalAddUserId = entity.ADDUSERID;
        var originalCreatedDate = entity.CREATEDDATE;
        var originalIsDeleted = entity.ISDELETED;

        var repository = new Mock<IChequeTypeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        Assert.Equal(originalId, entity.ID);
        Assert.Equal(originalAddUserId, entity.ADDUSERID);
        Assert.Equal(originalCreatedDate, entity.CREATEDDATE);
        Assert.Equal(originalIsDeleted, entity.ISDELETED);
    }

    [Fact]
    public async Task Handle_RecordDoesNotExist_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IChequeTypeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((TB_CHECK_TYPE?)null);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecordIsSoftDeleted_ThrowsNotFoundException_AndNeverCallsSaveChanges()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id, isDeleted: true);
        var repository = new Mock<IChequeTypeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(ValidCommand(id), CancellationToken.None));

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingRecord_CallsSaveChangesExactlyOnce()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IChequeTypeRepository>();
        repository.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new UpdateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), CancellationToken.None);

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToRepositoryAndUnitOfWork()
    {
        var id = Guid.NewGuid();
        var entity = ExistingEntity(id);
        var repository = new Mock<IChequeTypeRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        repository.Setup(r => r.GetForUpdateAsync(id, token)).ReturnsAsync(entity);

        var handler = new UpdateChequeTypeCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(id), token);

        repository.Verify(r => r.GetForUpdateAsync(id, token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
