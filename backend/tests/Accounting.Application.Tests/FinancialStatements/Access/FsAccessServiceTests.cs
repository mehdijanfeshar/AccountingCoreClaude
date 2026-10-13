using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements.Access;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.FinancialStatements.Access;

/// <summary>دسترسی سه‌بُعدی: کاربر × دامنهٔ واحد × عملیات. جدول خالی = همه مجاز.</summary>
public sealed class FsAccessServiceTests
{
    private readonly Mock<IFsPermissionRepository> _permissions = new();
    private readonly Mock<IUnitAccessReadRepository> _unitAccess = new();
    private readonly Mock<ICurrentUser> _user = new();

    public FsAccessServiceTests()
    {
        _user.SetupGet(u => u.UserId).Returns("u1");
        _unitAccess.Setup(a => a.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(FsTestData.Units());
    }

    private FsAccessService Sut(params TB_FS_PERMISSION[] rows)
    {
        _permissions.Setup(p => p.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rows.ToList());
        return new FsAccessService(_permissions.Object, _unitAccess.Object, _user.Object);
    }

    private static TB_FS_PERMISSION Perm(string user, string unit, FsOperation ops, bool includeSub = false)
        => new() { ID = Guid.NewGuid(), USERID = user, VAHEDCODE = unit, OPERATIONS = ops, INCLUDE_SUB = includeSub };

    [Fact]
    public async Task EmptyTable_GrantsEverything()
    {
        var ops = await Sut().GetOperationsAsync("101", default);

        Assert.Equal(FsAccessService.AllOperations, ops);
        await Sut().EnsureAsync(FsOperation.Publish, "101", default);
    }

    [Fact]
    public async Task TableHasRowsButNoneForUser_GrantsNothing()
    {
        var sut = Sut(Perm("someone-else", "101", FsOperation.View));

        Assert.Equal(FsOperation.None, await sut.GetOperationsAsync("101", default));
        await Assert.ThrowsAsync<FsAccessDeniedException>(() => sut.EnsureAsync(FsOperation.View, "101", default));
    }

    [Fact]
    public async Task ExactUnit_GrantsOnlyThoseOperations()
    {
        var sut = Sut(Perm("u1", "101", FsOperation.View | FsOperation.Prepare));

        Assert.Equal(FsOperation.View | FsOperation.Prepare, await sut.GetOperationsAsync("101", default));
        Assert.Equal(FsOperation.None, await sut.GetOperationsAsync("102", default));
    }

    [Fact]
    public async Task UserIdComparison_IsCaseInsensitive()
    {
        _user.SetupGet(u => u.UserId).Returns("ABC");
        var sut = Sut(Perm("abc", "101", FsOperation.View));

        Assert.Equal(FsOperation.View, await sut.GetOperationsAsync("101", default));
    }

    [Fact]
    public async Task IncludeSub_CoversDescendants_ButNotParentOrSiblingBranch()
    {
        var sut = Sut(Perm("u1", "10", FsOperation.View, includeSub: true));

        Assert.Equal(FsOperation.View, await sut.GetOperationsAsync("10", default));
        Assert.Equal(FsOperation.View, await sut.GetOperationsAsync("101", default));
        Assert.Equal(FsOperation.None, await sut.GetOperationsAsync("1", default));
        Assert.Equal(FsOperation.None, await sut.GetOperationsAsync("201", default));
    }

    [Fact]
    public async Task WithoutIncludeSub_DescendantsAreNotCovered()
    {
        var sut = Sut(Perm("u1", "10", FsOperation.View, includeSub: false));

        Assert.Equal(FsOperation.None, await sut.GetOperationsAsync("101", default));
    }

    [Fact]
    public async Task MultipleRows_AreUnioned()
    {
        var sut = Sut(
            Perm("u1", "10", FsOperation.View, includeSub: true),
            Perm("u1", "101", FsOperation.Approve));

        Assert.Equal(FsOperation.View | FsOperation.Approve, await sut.GetOperationsAsync("101", default));
        Assert.Equal(FsOperation.View, await sut.GetOperationsAsync("102", default));
    }

    [Fact]
    public async Task AdminFlag_ExpandsToAllOperations()
    {
        var sut = Sut(Perm("u1", "101", FsOperation.Admin));

        Assert.Equal(FsAccessService.AllOperations, await sut.GetOperationsAsync("101", default));
        await sut.EnsureAsync(FsOperation.ClosePeriod, "101", default);
    }

    [Fact]
    public async Task Ensure_RequiresEveryBitOfTheOperation()
    {
        var sut = Sut(Perm("u1", "101", FsOperation.View));

        await sut.EnsureAsync(FsOperation.View, "101", default);
        var ex = await Assert.ThrowsAsync<FsAccessDeniedException>(() => sut.EnsureAsync(FsOperation.View | FsOperation.Publish, "101", default));
        Assert.Contains("101", ex.PublicDetail);
    }

    [Fact]
    public async Task Ensure_NoneOperation_IsAlwaysAllowed_ForUserWithRow()
    {
        var sut = Sut(Perm("u1", "101", FsOperation.View));

        await sut.EnsureAsync(FsOperation.None, "202", default);
    }

    [Fact]
    public async Task Permissions_AreLoadedOnce_PerServiceInstance()
    {
        var sut = Sut(Perm("u1", "101", FsOperation.View));

        await sut.GetOperationsAsync("101", default);
        await sut.GetOperationsAsync("102", default);

        _permissions.Verify(p => p.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AllOperations_Is255()
    {
        Assert.Equal(255, (int)FsAccessService.AllOperations);
    }

    [Theory]
    [InlineData(FsOperation.View, "مشاهده")]
    [InlineData(FsOperation.Prepare, "تهیهٔ صورت‌ها")]
    [InlineData(FsOperation.EditTemplate, "تغییر قالب و تنظیمات")]
    [InlineData(FsOperation.ActivateTemplate, "فعال‌سازی قالب")]
    [InlineData(FsOperation.Approve, "تأیید")]
    [InlineData(FsOperation.Publish, "انتشار")]
    [InlineData(FsOperation.ClosePeriod, "بستن دوره")]
    [InlineData(FsOperation.Admin, "مدیریت دسترسی")]
    public void Label_ReturnsPersianTitle(FsOperation op, string expected)
    {
        Assert.Equal(expected, FsAccessService.Label(op));
    }
}
