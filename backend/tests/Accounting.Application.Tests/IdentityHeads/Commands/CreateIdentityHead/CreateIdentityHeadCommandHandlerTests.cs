using Accounting.Application.Common.Interfaces;
using Accounting.Application.IdentityHeads.Commands.Common;
using Accounting.Application.IdentityHeads.Commands.CreateIdentityHead;
using Accounting.Domain.Entity;
using Moq;

namespace Accounting.Application.Tests.IdentityHeads.Commands.CreateIdentityHead;

public sealed class CreateIdentityHeadCommandHandlerTests
{
    private const string Vahed = "1155";
    private const string Year = "1404";

    private static CreateIdentityHeadCommand ValidCommand(
        Guid groupId,
        IReadOnlyList<IdentityHeadFixItemInput>? fixItems = null)
        => new(groupId, Year, fixItems) { VahedCode = Vahed };

    private sealed record Harness(
        Mock<IIdentityHeadRepository> Repository,
        Mock<IUnitOfWork> UnitOfWork,
        CreateIdentityHeadCommandHandler Handler,
        List<TB_IDENTITYHEAD> StagedHeads,
        List<TB_IDENTITYFIXITEM> StagedFixItems);

    private static Harness CreateHarness(int nextSerial = 1)
    {
        var repository = new Mock<IIdentityHeadRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns("tester");

        var heads = new List<TB_IDENTITYHEAD>();
        var fixItems = new List<TB_IDENTITYFIXITEM>();

        repository
            .Setup(r => r.GetNextSerialAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nextSerial);
        repository
            .Setup(r => r.AddAsync(It.IsAny<TB_IDENTITYHEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_IDENTITYHEAD, CancellationToken>((h, _) => heads.Add(h))
            .Returns(Task.CompletedTask);
        repository
            .Setup(r => r.AddFixItemAsync(It.IsAny<TB_IDENTITYFIXITEM>(), It.IsAny<CancellationToken>()))
            .Callback<TB_IDENTITYFIXITEM, CancellationToken>((f, _) => fixItems.Add(f))
            .Returns(Task.CompletedTask);

        var handler = new CreateIdentityHeadCommandHandler(repository.Object, unitOfWork.Object, currentUser.Object);

        return new Harness(repository, unitOfWork, handler, heads, fixItems);
    }

    [Fact]
    public async Task Handle_StagesTheHead_WithServerAssignedAuditAndScope()
    {
        var groupId = Guid.NewGuid();
        var harness = CreateHarness();

        var id = await harness.Handler.Handle(ValidCommand(groupId), CancellationToken.None);

        var head = Assert.Single(harness.StagedHeads);
        Assert.Equal(id, head.ID);
        Assert.Equal(groupId, head.IDENTITYGROUPS_ID);
        Assert.Equal(Vahed, head.VAHEDCODE);
        Assert.Equal(Year, head.YEAR);
        Assert.Equal("tester", head.ADDUSERID);
        Assert.False(head.ISDELETED);
    }

    /// <summary>
    /// The serial is never taken from the caller — the reference UI does not send one either.
    /// </summary>
    [Fact]
    public async Task Handle_TakesTheSerialFromTheRepository_NotTheRequest()
    {
        var harness = CreateHarness(nextSerial: 42);

        await harness.Handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(42, Assert.Single(harness.StagedHeads).SERIAL);
    }

    [Fact]
    public async Task Handle_AsksForTheSerial_ScopedToGroupUnitAndYear()
    {
        var groupId = Guid.NewGuid();
        var harness = CreateHarness();

        await harness.Handler.Handle(ValidCommand(groupId), CancellationToken.None);

        harness.Repository.Verify(
            r => r.GetNextSerialAsync(groupId, Vahed, Year, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_StagesOneFixItemPerInput()
    {
        var subGroupA = Guid.NewGuid();
        var subGroupB = Guid.NewGuid();
        var harness = CreateHarness();

        await harness.Handler.Handle(
            ValidCommand(Guid.NewGuid(), new[]
            {
                new IdentityHeadFixItemInput(subGroupA, "الف"),
                new IdentityHeadFixItemInput(subGroupB, "ب"),
            }),
            CancellationToken.None);

        Assert.Equal(2, harness.StagedFixItems.Count);
        Assert.Contains(harness.StagedFixItems, f => f.IDENTITYSUBGRPS_ID == subGroupA && f.FIXITEMS_VALUE == "الف");
        Assert.Contains(harness.StagedFixItems, f => f.IDENTITYSUBGRPS_ID == subGroupB && f.FIXITEMS_VALUE == "ب");
    }

    /// <summary>
    /// Fix items must inherit their head's id and scope — never anything from the request, which
    /// carries neither.
    /// </summary>
    [Fact]
    public async Task Handle_FixItemsInheritHeadIdAndScope()
    {
        var harness = CreateHarness();

        var id = await harness.Handler.Handle(
            ValidCommand(Guid.NewGuid(), new[] { new IdentityHeadFixItemInput(Guid.NewGuid(), "مقدار") }),
            CancellationToken.None);

        var item = Assert.Single(harness.StagedFixItems);
        Assert.Equal(id, item.IDENTITYHEAD_ID);
        Assert.Equal(Vahed, item.VAHEDCODE);
        Assert.Equal(Year, item.YEAR);
        Assert.Equal("tester", item.ADDUSERID);
        Assert.False(item.ISDELETED);
    }

    [Fact]
    public async Task Handle_NullFixItems_StagesHeadOnly()
    {
        var harness = CreateHarness();

        await harness.Handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Single(harness.StagedHeads);
        Assert.Empty(harness.StagedFixItems);
    }

    /// <summary>
    /// One SaveChanges for the head and every item together — a شناسنامه must never be persisted
    /// without its values.
    /// </summary>
    [Fact]
    public async Task Handle_SavesOnce_ForHeadAndItemsTogether()
    {
        var harness = CreateHarness();

        await harness.Handler.Handle(
            ValidCommand(Guid.NewGuid(), new[]
            {
                new IdentityHeadFixItemInput(Guid.NewGuid(), "الف"),
                new IdentityHeadFixItemInput(Guid.NewGuid(), "ب"),
            }),
            CancellationToken.None);

        harness.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
