using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Access;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.FinancialStatements.Access;

public sealed class FsPermissionHandlersTests
{
    private readonly Mock<IFsPermissionRepository> _repo = new();
    private readonly Mock<IUnitAccessReadRepository> _unitAccess = new();
    private readonly Mock<IFsUnitScopeProvider> _scopes = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public FsPermissionHandlersTests()
    {
        _user.SetupGet(u => u.UserId).Returns("admin1");
        _unitAccess.Setup(a => a.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(FsTestData.Units());
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERMISSION>());
    }

    private FsPermissionHandlers Sut(FsUnitScope scope)
    {
        _scopes.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(scope);
        var access = new FsAccessService(_repo.Object, _unitAccess.Object, _user.Object);
        return new FsPermissionHandlers(_repo.Object, access, _scopes.Object, _uow.Object, _user.Object);
    }

    private static SaveFsPermissionCommand Save(Guid? id = null, string userId = "admin1", string unit = "101", FsOperation ops = FsOperation.Admin | FsOperation.View, bool sub = false)
        => new(id, userId, "  نام  ", unit, sub, ops) { VahedCode = "10" };

    [Fact]
    public async Task Save_UnitOutsideScope_IsDenied()
    {
        await Assert.ThrowsAsync<FsAccessDeniedException>(() => Sut(FsTestData.ProvinceScope()).Handle(Save(unit: "201"), default));
    }

    [Fact]
    public async Task Save_FirstRow_MustGiveAdminToCurrentUser()
    {
        var sut = Sut(FsTestData.ProvinceScope());

        // برای کاربر دیگر
        await Assert.ThrowsAsync<FsTemplateConflictException>(() => sut.Handle(Save(userId: "someone"), default));
        // بدون Admin
        await Assert.ThrowsAsync<FsTemplateConflictException>(() => sut.Handle(Save(ops: FsOperation.View), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Save_FirstRow_AdminForSelf_IsAdded_WithTrimmedFields()
    {
        TB_FS_PERMISSION? added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<TB_FS_PERMISSION>(), It.IsAny<CancellationToken>())).Callback<TB_FS_PERMISSION, CancellationToken>((p, _) => added = p);

        var id = await Sut(FsTestData.ProvinceScope()).Handle(Save(userId: " ADMIN1 ", sub: true), default);

        Assert.Equal(added!.ID, id);
        Assert.Equal("ADMIN1", added.USERID);
        Assert.Equal("نام", added.USERNAME);
        Assert.Equal("101", added.VAHEDCODE);
        Assert.True(added.INCLUDE_SUB);
        Assert.Equal("admin1", added.ADDUSERID);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_WhenTableNotEmpty_AllowsAnyUserAndOperations()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERMISSION>
        {
            new() { ID = Guid.NewGuid(), USERID = "admin1", VAHEDCODE = "10", OPERATIONS = FsOperation.Admin },
        });

        var id = await Sut(FsTestData.ProvinceScope()).Handle(Save(userId: "other", ops: FsOperation.View), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Save_Update_UnknownId_NotFound()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERMISSION>
        {
            new() { ID = Guid.NewGuid(), USERID = "admin1", VAHEDCODE = "10", OPERATIONS = FsOperation.Admin },
        });
        _repo.Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TB_FS_PERMISSION?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut(FsTestData.ProvinceScope()).Handle(Save(id: Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Save_Update_ChangesRow_AndStampsChangeUser()
    {
        var row = new TB_FS_PERMISSION { ID = Guid.NewGuid(), USERID = "old", VAHEDCODE = "10", OPERATIONS = FsOperation.View };
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERMISSION> { row });
        _repo.Setup(r => r.GetForUpdateAsync(row.ID, It.IsAny<CancellationToken>())).ReturnsAsync(row);

        await Sut(FsTestData.ProvinceScope()).Handle(Save(id: row.ID, userId: "new", ops: FsOperation.Approve), default);

        Assert.Equal("new", row.USERID);
        Assert.Equal(FsOperation.Approve, row.OPERATIONS);
        Assert.Equal("admin1", row.CHANGEUSERID);
        Assert.NotNull(row.UPDATEDDATE);
        _repo.Verify(r => r.AddAsync(It.IsAny<TB_FS_PERMISSION>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_SoftDeletes()
    {
        var row = new TB_FS_PERMISSION { ID = Guid.NewGuid() };
        _repo.Setup(r => r.GetForUpdateAsync(row.ID, It.IsAny<CancellationToken>())).ReturnsAsync(row);

        await Sut(FsTestData.ProvinceScope()).Handle(new DeleteFsPermissionCommand(row.ID) { VahedCode = "10" }, default);

        Assert.True(row.ISDELETED);
        Assert.Equal("admin1", row.CHANGEUSERID);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Unknown_NotFound()
    {
        _repo.Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((TB_FS_PERMISSION?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut(FsTestData.ProvinceScope()).Handle(new DeleteFsPermissionCommand(Guid.NewGuid()) { VahedCode = "10" }, default));
    }

    [Fact]
    public async Task List_ReturnsOnlyRowsInScope_OrderedByUser()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERMISSION>
        {
            new() { ID = Guid.NewGuid(), USERID = "b", VAHEDCODE = "101", OPERATIONS = FsOperation.View },
            new() { ID = Guid.NewGuid(), USERID = "a", VAHEDCODE = "10", OPERATIONS = FsOperation.View },
            new() { ID = Guid.NewGuid(), USERID = "c", VAHEDCODE = "201", OPERATIONS = FsOperation.View },
        });

        var result = await Sut(FsTestData.ProvinceScope()).Handle(new GetFsPermissionsQuery { VahedCode = "10" }, default);

        Assert.Equal(new[] { "a", "b" }, result.Select(r => r.UserId).ToArray());
    }

    [Fact]
    public async Task MyOperations_DelegatesToAccessService()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERMISSION>
        {
            new() { ID = Guid.NewGuid(), USERID = "admin1", VAHEDCODE = "101", OPERATIONS = FsOperation.View | FsOperation.Prepare },
        });

        var ops = await Sut(FsTestData.ProvinceScope()).Handle(new GetMyFsOperationsQuery { VahedCode = "101" }, default);

        Assert.Equal(FsOperation.View | FsOperation.Prepare, ops);
    }
}

public sealed class SaveFsPermissionCommandValidatorTests
{
    private readonly SaveFsPermissionCommandValidator _sut = new();

    private static SaveFsPermissionCommand Cmd(string user = "u1", string unit = "101", FsOperation ops = FsOperation.View)
        => new(null, user, null, unit, false, ops);

    [Fact]
    public void Valid_Passes() => Assert.True(_sut.Validate(Cmd()).IsValid);

    [Fact]
    public void Combined_Flags_Pass() => Assert.True(_sut.Validate(Cmd(ops: FsOperation.View | FsOperation.Admin)).IsValid);

    [Fact]
    public void None_Fails() => Assert.False(_sut.Validate(Cmd(ops: FsOperation.None)).IsValid);

    [Fact]
    public void BitsOutsideKnownRange_Fail() => Assert.False(_sut.Validate(Cmd(ops: (FsOperation)256)).IsValid);

    [Theory]
    [InlineData("", "101")]
    [InlineData("12345678901", "101")]
    [InlineData("u1", "")]
    [InlineData("u1", "12345")]
    public void BadUserOrUnit_Fails(string user, string unit) => Assert.False(_sut.Validate(Cmd(user, unit)).IsValid);
}
