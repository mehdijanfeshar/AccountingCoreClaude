using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Periods;
using Accounting.Application.OperationTemplates;
using Accounting.Domain.Entity;
using Accounting.Domain.OperationTemplates;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.OperationTemplates;

public class SavedReportHandlersTests
{
    private readonly Mock<ISavedReportRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IHeadquartersAccessService> _hq = new();

    public SavedReportHandlersTests()
    {
        _user.Setup(u => u.UserId).Returns("u1");
        _hq.Setup(h => h.HasAbilityAsync(AbilityCatalog.SavedReportsDefine, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private CreateSavedReportCommand Valid(string code = "trial_1") =>
        new(code, " گزارش ", " توضیح ", null, "trial-balance", "{\"a\":1}", ["21", "17"]);

    [Fact]
    public async Task Create_WithoutAbility_Denied()
    {
        _hq.Setup(h => h.HasAbilityAsync(AbilityCatalog.SavedReportsDefine, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() =>
            new CreateSavedReportHandler(_repo.Object, _uow.Object, _user.Object, _hq.Object).Handle(Valid(), default));
    }

    [Fact]
    public async Task Create_WithoutHqService_FallsBackToSetadAdminRole()
    {
        var h = new CreateSavedReportHandler(_repo.Object, _uow.Object, _user.Object);
        await Assert.ThrowsAsync<RoleAccessDeniedException>(() => h.Handle(Valid(), default));

        _user.Setup(u => u.IsInRole(AppRoles.SetadAdmin)).Returns(true);
        Assert.True((await h.Handle(Valid(), default)).Success);
    }

    [Fact]
    public async Task Create_Valid_NormalizesAndPersists()
    {
        SavedReport? added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<SavedReport>(), It.IsAny<CancellationToken>()))
            .Callback<SavedReport, CancellationToken>((r, _) => added = r);

        var res = await new CreateSavedReportHandler(_repo.Object, _uow.Object, _user.Object, _hq.Object).Handle(Valid(), default);

        Assert.True(res.Success);
        Assert.Equal("TRIAL_1", added!.Code);
        Assert.Equal("گزارش", added.Title);
        Assert.Equal("توضیح", added.Description);
        Assert.Null(added.Keywords);
        Assert.Equal("17,21", added.AllowedVahedTypes);
        Assert.True(added.IsActive);
        Assert.Equal("u1", added.CreatedBy);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("X", "t", "trial-balance", "{}")]          // کد کوتاه
    [InlineData("OK_CODE", "", "trial-balance", "{}")]     // عنوان خالی
    [InlineData("OK_CODE", "t", "unknown-kind", "{}")]     // نوع نامعتبر
    [InlineData("OK_CODE", "t", "matrix", "[1]")]          // JSON غیر شیء
    [InlineData("OK_CODE", "t", "matrix", "not json")]     // JSON خراب
    public async Task Create_Invalid_ReturnsErrorsAndDoesNotSave(string code, string title, string kind, string json)
    {
        var res = await new CreateSavedReportHandler(_repo.Object, _uow.Object, _user.Object, _hq.Object)
            .Handle(new(code, title, null, null, kind, json), default);
        Assert.False(res.Success);
        Assert.NotEmpty(res.Errors);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_DuplicateCode_Rejected()
    {
        _repo.Setup(r => r.CodeExistsForOtherAsync("TRIAL_1", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var res = await new CreateSavedReportHandler(_repo.Object, _uow.Object, _user.Object, _hq.Object).Handle(Valid(), default);
        Assert.False(res.Success);
        _repo.Verify(r => r.AddAsync(It.IsAny<SavedReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_RaceOnUniqueKey_ReturnsFriendlyError()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateKeyException("dup", new Exception()));
        var res = await new CreateSavedReportHandler(_repo.Object, _uow.Object, _user.Object, _hq.Object).Handle(Valid(), default);
        Assert.False(res.Success);
        Assert.Contains("قبلاً ثبت", res.Errors.Single());
    }

    [Fact]
    public async Task Update_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateSavedReportHandler(_repo.Object, _uow.Object, _user.Object, _hq.Object)
                .Handle(new UpdateSavedReportCommand("OK_CODE", "t", null, null, "matrix", "{}") { Id = Guid.NewGuid() }, default));

    [Fact]
    public async Task Update_Valid_ChangesFields_ExcludesSelfFromCodeUniqueness()
    {
        var id = Guid.NewGuid();
        var entity = new SavedReport { Id = id, Code = "OLD_CODE", Title = "old" };
        _repo.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var res = await new UpdateSavedReportHandler(_repo.Object, _uow.Object, _user.Object, _hq.Object)
            .Handle(new UpdateSavedReportCommand("new_code", "جدید", "  ", null, "voucher-review", "{}") { Id = id }, default);

        Assert.True(res.Success);
        Assert.Equal("NEW_CODE", entity.Code);
        Assert.Equal("جدید", entity.Title);
        Assert.Null(entity.Description);
        _repo.Verify(r => r.CodeExistsForOtherAsync("NEW_CODE", id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetActive_TogglesAndSaves_NotFoundThrows()
    {
        var id = Guid.NewGuid();
        var h = new SetSavedReportActiveHandler(_repo.Object, _uow.Object, _user.Object, _hq.Object);
        await Assert.ThrowsAsync<NotFoundException>(() => h.Handle(new SetSavedReportActiveCommand(false) { Id = id }, default));

        var entity = new SavedReport { Id = id, IsActive = true };
        _repo.Setup(r => r.GetForUpdateAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        await h.Handle(new SetSavedReportActiveCommand(false) { Id = id }, default);
        Assert.False(entity.IsActive);
    }

    [Fact]
    public async Task Get_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetSavedReportHandler(_repo.Object).Handle(new GetSavedReportQuery(Guid.NewGuid()), default));

    [Fact]
    public async Task ListForUnit_FiltersByUnitType()
    {
        var unitAccess = new Mock<IUnitAccessReadRepository>();
        unitAccess.Setup(u => u.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UnitNode>
        {
            new(Guid.NewGuid(), "0002", "A", null, false, "17"),
        });
        var policy = new AssistantUnitPolicy(unitAccess.Object, new FsPeriodGuard(Mock.Of<IFsPeriodRepository>(), unitAccess.Object));
        _repo.Setup(r => r.ListAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(new List<SavedReport>
        {
            new() { Code = "ALL", Title = "همه" },
            new() { Code = "MINE", Title = "من", AllowedVahedTypes = "17,21" },
            new() { Code = "OTHER", Title = "دیگر", AllowedVahedTypes = "5" },
        });

        var res = await new ListSavedReportsHandler(_repo.Object, policy)
            .Handle(new ListSavedReportsQuery { VahedCode = "0002" }, default);

        Assert.Equal(new[] { "ALL", "MINE" }, res.Select(r => r.Code));
    }
}

public class AssistantUnitPolicyTests
{
    private readonly Mock<IUnitAccessReadRepository> _units = new();
    private readonly Mock<IFsPeriodRepository> _periods = new();

    public AssistantUnitPolicyTests()
    {
        _units.Setup(u => u.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UnitNode>
        {
            new(Guid.NewGuid(), "0002", "A", null, false, "17"),
            new(Guid.NewGuid(), "0003", "B", null, false, null),
        });
        _periods.Setup(p => p.GetForYearAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<TB_FS_PERIOD>());
    }

    private AssistantUnitPolicy Policy() => new(_units.Object, new FsPeriodGuard(_periods.Object, _units.Object));

    [Theory]
    [InlineData(null, "17", true)]            // بدون محدودیت
    [InlineData("17,21", "17", true)]
    [InlineData("17,21", "5", false)]
    [InlineData("17", null, false)]            // نوع واحد نامعلوم و محدودیت ⇒ ممنوع
    public void IsAllowed(string? allowed, string? unitType, bool expected)
        => Assert.Equal(expected, AssistantUnitPolicy.IsAllowed(new OperationTemplate { AllowedVahedTypes = allowed }, unitType));

    [Fact]
    public async Task Filter_NoRestrictions_ReturnsSameListWithoutLoadingUnits()
    {
        IReadOnlyList<OperationTemplate> list = [new() { Code = "A" }];
        var res = await Policy().FilterAsync(list, "0002", default);
        Assert.Same(list, res);
        _units.Verify(u => u.GetAllUnitsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Filter_Restricted_KeepsOnlyMatching()
    {
        IReadOnlyList<OperationTemplate> list =
        [
            new() { Code = "A" },
            new() { Code = "B", AllowedVahedTypes = "17" },
            new() { Code = "C", AllowedVahedTypes = "5" },
        ];
        var res = await Policy().FilterAsync(list, "0002", default);
        Assert.Equal(new[] { "A", "B" }, res.Select(t => t.Code));
        res = await Policy().FilterAsync(list, "0003", default); // واحد بدون نوع
        Assert.Equal(new[] { "A" }, res.Select(t => t.Code));
    }

    [Fact]
    public async Task TemplateNotAllowed_ReturnsTemplateInactiveError()
    {
        var t = new OperationTemplate { Title = "الگو", AllowedVahedTypes = "5" };
        var err = await Policy().TemplateNotAllowedAsync(t, "0002", default);
        Assert.Equal(EngineErrorCode.TemplateInactive, err!.Code);
        Assert.Null(await Policy().TemplateNotAllowedAsync(new OperationTemplate { Title = "x" }, "0002", default));
        Assert.Null(await Policy().TemplateNotAllowedAsync(new OperationTemplate { AllowedVahedTypes = "17" }, "0002", default));
    }

    [Fact]
    public async Task PeriodLocked_UnlockedOrNoRow_ReturnsNull()
    {
        Assert.Null(await Policy().PeriodLockedAsync("0002", new DateOnly(2026, 3, 21), default));
    }

    [Fact]
    public async Task PeriodLocked_LockedPeriod_ReturnsVoucherDateError()
    {
        _periods.Setup(p => p.GetForYearAsync("1405", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_FS_PERIOD> { new() { VAHEDCODE = "0002", STATE = FsPeriodState.Locked } });
        var err = await Policy().PeriodLockedAsync("0002", new DateOnly(2026, 3, 21), default);
        Assert.Equal("voucherDate", err!.ParameterKey);
        Assert.Contains("1405", err.Message);
    }

    [Fact]
    public async Task PeriodLocked_ParentLocked_AlsoBlocks()
    {
        var parent = Guid.NewGuid();
        _units.Setup(u => u.GetAllUnitsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UnitNode>
        {
            new(parent, "0001", "P", null, true, "10"),
            new(Guid.NewGuid(), "0002", "A", parent, false, "17"),
        });
        _periods.Setup(p => p.GetForYearAsync("1405", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_FS_PERIOD> { new() { VAHEDCODE = "0001", STATE = FsPeriodState.Locked } });
        Assert.NotNull(await Policy().PeriodLockedAsync("0002", new DateOnly(2026, 3, 21), default));
    }

    [Fact]
    public async Task PeriodLocked_TableMissing_DoesNotBlock()
    {
        _periods.Setup(p => p.GetForYearAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ORA-00942"));
        Assert.Null(await Policy().PeriodLockedAsync("0002", new DateOnly(2026, 3, 21), default));
    }
}
