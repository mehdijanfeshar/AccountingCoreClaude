using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.FinancialStatements;
using Accounting.Application.Queries.UnitAccess;
using Moq;

namespace Accounting.Application.Tests.FinancialStatements;

/// <summary>قواعد تفکیک واحد قالب‌ها (docs/fs-module.md §۸): دیدن، تغییر، اولویت، گروه.</summary>
public sealed class FsUnitScopeTests
{
    [Fact]
    public void Build_Ancestors_AreSelfFirstThenUp()
    {
        var scope = FsTestData.ProvinceScope();

        Assert.Equal(new[] { "10", "1" }, scope.AncestorsOrSelf.ToArray());
        Assert.False(scope.IsHeadquarters);
        Assert.True(FsTestData.HqScope().IsHeadquarters);
    }

    [Fact]
    public void Build_Names_And_TypeCodes_AreExposed()
    {
        var scope = FsTestData.ProvinceScope();

        Assert.Equal("استان دیگر", scope.Names["20"]);
        Assert.Equal("P", scope.TypeCodes["10"]);
        Assert.Null(scope.TypeCodes["101"]);
    }

    [Fact]
    public void CanSee_SharedOwnAncestorAndDescendant_ButNotOtherBranch()
    {
        var scope = FsTestData.ProvinceScope();

        Assert.True(scope.CanSee(null));     // مشترک
        Assert.True(scope.CanSee("10"));     // خود
        Assert.True(scope.CanSee("1"));      // جد
        Assert.True(scope.CanSee("101"));    // زیرمجموعه
        Assert.False(scope.CanSee("20"));    // شاخهٔ دیگر
        Assert.False(scope.CanSee("201"));
    }

    [Fact]
    public void CanEdit_SharedOnlyHeadquarters_OtherwiseSelfOrDescendants()
    {
        var province = FsTestData.ProvinceScope();
        var hq = FsTestData.HqScope();

        Assert.False(province.CanEdit(null));
        Assert.True(hq.CanEdit(null));
        Assert.True(province.CanEdit("10"));
        Assert.True(province.CanEdit("101"));
        Assert.False(province.CanEdit("1"));    // قالب جد قابل تغییر نیست
        Assert.False(province.CanEdit("20"));
        Assert.True(hq.CanEdit("201"));         // ستاد = همه
    }

    [Fact]
    public void PriorityOf_SelfBeforeParentBeforeShared_UnrelatedIsNull()
    {
        var scope = FsTestData.ProvinceScope();

        Assert.Equal(0, scope.PriorityOf("10"));
        Assert.Equal(1, scope.PriorityOf("1"));
        Assert.Equal(int.MaxValue, scope.PriorityOf(null));
        Assert.Null(scope.PriorityOf("20"));
    }

    [Fact]
    public void GroupOf_IsFirstLevelChildUnderTheScopeUnit()
    {
        var hq = FsTestData.HqScope();
        var province = FsTestData.ProvinceScope();

        Assert.Equal("10", hq.GroupOf("101"));
        Assert.Equal("10", hq.GroupOf("10"));
        Assert.Equal("20", hq.GroupOf("201"));
        Assert.Equal("101", province.GroupOf("101"));
        Assert.Equal("10", province.GroupOf("10"));   // خود واحد (بدون گروه) = کد خودش
        Assert.Equal("10", province.GroupOf("unknown"));
    }

    [Fact]
    public void EnsureCanEdit_Allowed_DoesNotThrow()
    {
        FsTestData.ProvinceScope().EnsureCanEdit("101");
        FsTestData.HqScope().EnsureCanEdit(null);
    }

    [Fact]
    public void EnsureCanEdit_SharedByNonHeadquarters_Throws()
    {
        var ex = Assert.Throws<FsAccessDeniedException>(() => FsTestData.ProvinceScope().EnsureCanEdit(null));

        Assert.Contains("مشترک", ex.PublicDetail);
    }

    [Fact]
    public void EnsureCanEdit_OtherUnitsTemplate_Throws()
    {
        var ex = Assert.Throws<FsAccessDeniedException>(() => FsTestData.ProvinceScope().EnsureCanEdit("20"));

        Assert.Contains("واحد دیگری", ex.PublicDetail);
    }

    [Fact]
    public void Build_UnknownUnit_StillWorks_WithOnlyItself()
    {
        var scope = FsUnitScope.Build("999", FsTestData.Units(), new HashSet<string> { "999" });

        Assert.Equal(new[] { "999" }, scope.AncestorsOrSelf.ToArray());
        Assert.False(scope.IsHeadquarters);
    }

    [Fact]
    public void Build_CyclicParents_DoesNotLoopForever()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var units = new List<UnitNode> { new(a, "A", "الف", b, false), new(b, "B", "ب", a, false) };

        var scope = FsUnitScope.Build("A", units, new HashSet<string> { "A", "B" });

        Assert.Equal(new[] { "A", "B" }, scope.AncestorsOrSelf.ToArray());
    }

    [Fact]
    public async Task Provider_AlwaysIncludesOwnUnit_EvenIfRepositoryReturnsNothing()
    {
        var repo = new Mock<IUnitAccessReadRepository>();
        repo.Setup(r => r.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(FsTestData.Units());
        repo.Setup(r => r.GetAccessibleUnitsAsync("10", It.IsAny<CancellationToken>())).ReturnsAsync(new List<AccessibleUnitDto>());

        var scope = await new FsUnitScopeProvider(repo.Object).GetAsync("10");

        Assert.Contains("10", scope.Accessible);
        Assert.Single(scope.Accessible);
    }
}
