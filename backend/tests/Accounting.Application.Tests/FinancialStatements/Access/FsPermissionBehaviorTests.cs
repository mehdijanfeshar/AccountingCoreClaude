using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Access;
using Accounting.Application.FinancialStatements.Approvals;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using MediatR;
using Moq;

namespace Accounting.Application.Tests.FinancialStatements.Access;

/// <summary>
/// FsPermissionBehavior فقط درخواست‌های ماژول FinancialStatements را (از روی namespace) کنترل می‌کند؛
/// عملیات لازم از FsRequires، و پیش‌فرض «مشاهده».
/// </summary>
public sealed class FsPermissionBehaviorTests
{
    // درخواست‌های بیرون از ماژول (namespace تست) — نباید کنترل شوند.
    public sealed record OutsideModuleQuery : IRequest<int>, IVahedScopedQuery
    {
        public string VahedCode { get; set; } = string.Empty;
    }

    private readonly Mock<IUnitAccessReadRepository> _unitAccess = new();
    private readonly Mock<ICurrentUser> _user = new();

    public FsPermissionBehaviorTests()
    {
        _user.SetupGet(u => u.UserId).Returns("u1");
        _unitAccess.Setup(a => a.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(FsTestData.Units());
    }

    private FsAccessService Access(params TB_FS_PERMISSION[] rows)
    {
        // هر سرویس مخزن مستقل خودش را دارد (تا چند سرویس در یک تست روی هم Setup نزنند).
        var permissions = new Mock<IFsPermissionRepository>();
        permissions.Setup(p => p.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rows.ToList());
        return new FsAccessService(permissions.Object, _unitAccess.Object, _user.Object);
    }

    private static TB_FS_PERMISSION Perm(string unit, FsOperation ops)
        => new() { ID = Guid.NewGuid(), USERID = "u1", VAHEDCODE = unit, OPERATIONS = ops };

    private static async Task<(bool NextCalled, Exception? Error)> Run<TReq, TRes>(FsPermissionBehavior<TReq, TRes> sut, TReq request)
        where TReq : notnull
    {
        var called = false;
        try
        {
            await sut.Handle(request, _ => { called = true; return Task.FromResult(default(TRes)!); }, default);
            return (called, null);
        }
        catch (Exception ex)
        {
            return (called, ex);
        }
    }

    [Fact]
    public async Task RequestOutsideFinancialStatementsModule_IsNotChecked()
    {
        // کاربر ردیفی دارد که هیچ‌چیز نمی‌دهد؛ ولی درخواست بیرون از ماژول است.
        var sut = new FsPermissionBehavior<OutsideModuleQuery, int>(Access(Perm("999", FsOperation.View)));

        var (called, error) = await Run(sut, new OutsideModuleQuery { VahedCode = "101" });

        Assert.True(called);
        Assert.Null(error);
    }

    [Fact]
    public async Task FsRequestWithoutAttribute_RequiresView()
    {
        // GetFsApprovalStepsQuery بدون FsRequires = «مشاهده».
        var allowed = new FsPermissionBehavior<GetFsApprovalStepsQuery, IReadOnlyList<FsApprovalStepDto>>(Access(Perm("101", FsOperation.View)));
        var denied = new FsPermissionBehavior<GetFsApprovalStepsQuery, IReadOnlyList<FsApprovalStepDto>>(Access(Perm("101", FsOperation.Prepare)));
        var req = new GetFsApprovalStepsQuery(null) { VahedCode = "101" };

        Assert.Null((await Run(allowed, req)).Error);
        var (called, error) = await Run(denied, req);
        Assert.False(called);
        Assert.IsType<FsAccessDeniedException>(error);
    }

    [Fact]
    public async Task FsRequestWithAttribute_RequiresThatOperation()
    {
        // CreateFsApprovalStepCommand: EditTemplate
        var req = new CreateFsApprovalStepCommand(FsFramework.Pension, false, 1, "عنوان", null) { VahedCode = "101" };

        var viewOnly = new FsPermissionBehavior<CreateFsApprovalStepCommand, Guid>(Access(Perm("101", FsOperation.View)));
        var editor = new FsPermissionBehavior<CreateFsApprovalStepCommand, Guid>(Access(Perm("101", FsOperation.EditTemplate)));

        var (calledDenied, error) = await Run(viewOnly, req);
        Assert.False(calledDenied);
        Assert.IsType<FsAccessDeniedException>(error);
        Assert.True((await Run(editor, req)).NextCalled);
    }

    [Fact]
    public async Task AdminOnlyQuery_DeniedForViewer_AllowedForAdmin()
    {
        var req = new GetFsPermissionsQuery { VahedCode = "101" };

        var viewer = new FsPermissionBehavior<GetFsPermissionsQuery, IReadOnlyList<FsPermissionDto>>(Access(Perm("101", FsOperation.View)));
        var admin = new FsPermissionBehavior<GetFsPermissionsQuery, IReadOnlyList<FsPermissionDto>>(Access(Perm("101", FsOperation.Admin)));

        Assert.IsType<FsAccessDeniedException>((await Run(viewer, req)).Error);
        Assert.True((await Run(admin, req)).NextCalled);
    }

    [Fact]
    public async Task NoneOperation_Query_PassesEvenWithoutAnyRightsForThatUnit()
    {
        // GetMyFsOperationsQuery با FsRequires(None) — برای خواندن حقوق خود کاربر.
        var sut = new FsPermissionBehavior<GetMyFsOperationsQuery, FsOperation>(Access(Perm("999", FsOperation.View)));

        Assert.True((await Run(sut, new GetMyFsOperationsQuery { VahedCode = "101" })).NextCalled);
    }

    [Fact]
    public async Task EmptyVahedCode_SkipsCheck()
    {
        var sut = new FsPermissionBehavior<GetFsPermissionsQuery, IReadOnlyList<FsPermissionDto>>(Access(Perm("999", FsOperation.View)));

        Assert.True((await Run(sut, new GetFsPermissionsQuery { VahedCode = "" })).NextCalled);
    }

    [Fact]
    public async Task EmptyPermissionTable_AllowsEveryone()
    {
        var sut = new FsPermissionBehavior<GetFsPermissionsQuery, IReadOnlyList<FsPermissionDto>>(Access());

        Assert.True((await Run(sut, new GetFsPermissionsQuery { VahedCode = "101" })).NextCalled);
    }
}
