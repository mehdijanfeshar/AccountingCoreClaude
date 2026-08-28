using Accounting.Application.IdentitySubGroups.Commands.CreateIdentitySubGroup;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.IdentitySubGroups.Commands.CreateIdentitySubGroup;

public sealed class CreateIdentitySubGroupCommandHandlerTests
{
    private static CreateIdentitySubGroupCommand ValidCommand() => new(
        IdentyGroupsId: Guid.NewGuid(),
        SubgrpsDesc: "Sub group",
        SubgrpsLen: 4,
        SumFlag: true,
        Fixed: false,
        SubgrpsType: true,
        VahedCode: "0100",
        Year: "1403",
        IdentySubGroupsCode: "01");

    private static Mock<ICurrentUser> CurrentUserMock(string userId = "user1")
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);
        return currentUser;
    }

    [Fact]
    public async Task Handle_MapsCommandFieldsOntoStagedEntity()
    {
        var repository = new Mock<IIdentitySubGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_IDENTITYSUBGRP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_IDENTITYSUBGRP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_IDENTITYSUBGRP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateIdentitySubGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);
        var command = ValidCommand();

        await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal(command.IdentyGroupsId, staged!.IDENTYGROUPS_ID);
        Assert.Equal(command.SubgrpsDesc, staged.SUBGRPS_DESC);
        Assert.Equal(command.SubgrpsLen, staged.SUBGRPS_LEN);
        Assert.Equal(command.SumFlag, staged.SUMFLAG);
        Assert.Equal(command.Fixed, staged.FIXED);
        Assert.Equal(command.SubgrpsType, staged.SUBGRPS_TYPE);
        Assert.Equal(command.VahedCode, staged.VAHEDCODE);
        Assert.Equal(command.Year, staged.YEAR);
        Assert.Equal(command.IdentySubGroupsCode, staged.IDENTYSUBGROUPS_CODE);
    }

    [Fact]
    public async Task Handle_SetsAddUserIdFromCurrentUser_NeverFromRequest()
    {
        var repository = new Mock<IIdentitySubGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock("srvusr01");
        TB_IDENTITYSUBGRP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_IDENTITYSUBGRP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_IDENTITYSUBGRP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateIdentitySubGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.Equal("srvusr01", staged!.ADDUSERID);
        currentUser.VerifyGet(u => u.UserId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_SetsIsDeletedFalseAndNonDefaultCreatedDate()
    {
        var repository = new Mock<IIdentitySubGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_IDENTITYSUBGRP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_IDENTITYSUBGRP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_IDENTITYSUBGRP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateIdentitySubGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(staged);
        Assert.False(staged!.ISDELETED);
        Assert.NotEqual(default, staged.CREATEDDATE);
    }

    [Fact]
    public async Task Handle_ReturnsSameGuidAssignedToStagedEntity_AndItIsNonEmpty()
    {
        var repository = new Mock<IIdentitySubGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        TB_IDENTITYSUBGRP? staged = null;
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_IDENTITYSUBGRP>(), It.IsAny<CancellationToken>()))
            .Callback<TB_IDENTITYSUBGRP, CancellationToken>((entity, _) => staged = entity)
            .Returns(Task.CompletedTask);

        var handler = new CreateIdentitySubGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(staged);
        Assert.Equal(staged!.ID, result);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncExactlyOnceAndSaveChangesExactlyOnce()
    {
        var repository = new Mock<IIdentitySubGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();

        var handler = new CreateIdentitySubGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        repository.Verify(
            r => r.AddAsync(It.IsAny<TB_IDENTITYSUBGRP>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsAddAsyncBeforeSaveChanges()
    {
        var repository = new Mock<IIdentitySubGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        var callOrder = new List<string>();

        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_IDENTITYSUBGRP>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AddAsync"))
            .Returns(Task.CompletedTask);
        unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        var handler = new CreateIdentitySubGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(new[] { "AddAsync", "SaveChangesAsync" }, callOrder);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToBothDependencies()
    {
        var repository = new Mock<IIdentitySubGroupRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = CurrentUserMock();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var handler = new CreateIdentitySubGroupCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        await handler.Handle(ValidCommand(), token);

        repository.Verify(r => r.AddAsync(It.IsAny<TB_IDENTITYSUBGRP>(), token), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(token), Times.Once);
    }
}
