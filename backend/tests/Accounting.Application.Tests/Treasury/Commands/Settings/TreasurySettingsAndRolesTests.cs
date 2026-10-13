using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Commands.Treasury.CreateTreasuryRole;
using Accounting.Application.Commands.Treasury.DeleteTreasuryRole;
using Accounting.Application.Commands.Treasury.UpsertTreasurySetting;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.Accounts;
using Accounting.Application.Queries.TafsilGroups;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.Settings;

/// <summary>نقش‌های خزانه‌داری (bootstrap، upsert نقش) و تنظیمات خزانه‌داری (فقط مدیر مالی).</summary>
public sealed class TreasurySettingsAndRolesTests
{
    private const string Vahed = "V1";

    private readonly Mock<ITreasuryRoleRepository> _roles = new();
    private readonly Mock<ITreasuryRoleAuthorizer> _authorizer = new();
    private readonly Mock<ITreasurySettingRepository> _settings = new();
    private readonly Mock<ITafsilGroupReadRepository> _groups = new();
    private readonly Mock<IAccountCodeReadRepository> _accounts = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public TreasurySettingsAndRolesTests()
    {
        _user.SetupGet(u => u.UserId).Returns("caller");
        _roles.Setup(r => r.GetActiveRolesAsync(Vahed, "caller", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TreasuryRole>());
    }

    // ── CreateTreasuryRole ──────────────────────────────────────────────────

    private CreateTreasuryRoleCommandHandler RoleSut() => new(_roles.Object, _uow.Object, _user.Object);

    private static CreateTreasuryRoleCommand RoleCmd(TreasuryRole role = TreasuryRole.Treasurer)
        => new("user2", "علی", role) { VahedCode = Vahed };

    [Fact]
    public async Task CreateRole_CallerIsFinanceManager_AddsNewRole()
    {
        _roles.Setup(r => r.GetActiveRolesAsync(Vahed, "caller", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TreasuryRole> { TreasuryRole.FinanceManager });
        TB_TR_ROLE? added = null;
        _roles.Setup(r => r.AddAsync(It.IsAny<TB_TR_ROLE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_ROLE, CancellationToken>((r, _) => added = r).Returns(Task.CompletedTask);

        var id = await RoleSut().Handle(RoleCmd(), default);

        Assert.Equal(id, added!.ID);
        Assert.Equal("user2", added.USERID);
        Assert.Equal(TreasuryRole.Treasurer, added.ROLE);
        Assert.Equal(Vahed, added.VAHEDCODE);
        Assert.Equal("caller", added.ADDUSERID);
        Assert.False(added.ISDELETED);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // مدیر مالی است، پس نیازی به بررسی bootstrap نیست
        _roles.Verify(r => r.HasActiveFinanceManagerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateRole_NotFinanceManager_AndUnitHasOne_Throws()
    {
        _roles.Setup(r => r.HasActiveFinanceManagerAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(() => RoleSut().Handle(RoleCmd(), default));

        _roles.Verify(r => r.AddAsync(It.IsAny<TB_TR_ROLE>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateRole_Bootstrap_NoFinanceManagerInUnit_AnyoneMayCreate()
    {
        _roles.Setup(r => r.HasActiveFinanceManagerAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var id = await RoleSut().Handle(RoleCmd(TreasuryRole.FinanceManager), default);

        Assert.NotEqual(Guid.Empty, id);
        _roles.Verify(r => r.AddAsync(It.IsAny<TB_TR_ROLE>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRole_ExistingSoftDeletedRow_IsReactivatedInsteadOfDuplicated()
    {
        _roles.Setup(r => r.HasActiveFinanceManagerAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var existing = new TB_TR_ROLE { ID = Guid.NewGuid(), ISDELETED = true, USER_NAME = "old" };
        _roles.Setup(r => r.GetByVahedUserAndRoleAsync(Vahed, "user2", TreasuryRole.Treasurer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var id = await RoleSut().Handle(RoleCmd(), default);

        Assert.Equal(existing.ID, id);
        Assert.False(existing.ISDELETED);
        Assert.Equal("علی", existing.USER_NAME);
        Assert.Equal("caller", existing.CHANGEUSERID);
        _roles.Verify(r => r.AddAsync(It.IsAny<TB_TR_ROLE>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CreateRoleValidator_Rules()
    {
        var v = new CreateTreasuryRoleCommandValidator();
        Assert.True(v.Validate(RoleCmd()).IsValid);
        Assert.False(v.Validate(RoleCmd() with { UserId = "" }).IsValid);
        Assert.False(v.Validate(RoleCmd() with { UserId = "12345678901" }).IsValid);
        Assert.False(v.Validate(RoleCmd() with { UserName = new string('a', 201) }).IsValid);
        Assert.False(v.Validate(RoleCmd((TreasuryRole)99)).IsValid);
        Assert.False(v.Validate(RoleCmd() with { VahedCode = "" }).IsValid);
    }

    // ── DeleteTreasuryRole ──────────────────────────────────────────────────

    private DeleteTreasuryRoleCommandHandler DeleteSut() => new(_roles.Object, _authorizer.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task DeleteRole_Valid_SoftDeletes()
    {
        var id = Guid.NewGuid();
        var role = new TB_TR_ROLE { ID = id };
        _roles.Setup(r => r.GetForUpdateAsync(id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(role);

        await DeleteSut().Handle(new DeleteTreasuryRoleCommand(id) { VahedCode = Vahed }, default);

        Assert.True(role.ISDELETED);
        Assert.Equal("caller", role.CHANGEUSERID);
        _authorizer.Verify(a => a.EnsureHasRoleAsync(Vahed, It.Is<IReadOnlyCollection<TreasuryRole>>(c => c.Contains(TreasuryRole.FinanceManager)), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRole_NotFinanceManager_ThrowsBeforeLookup()
    {
        _authorizer.Setup(a => a.EnsureHasRoleAsync(Vahed, It.IsAny<IReadOnlyCollection<TreasuryRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryRoleRequiredException(Vahed, TreasuryRole.FinanceManager));

        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(
            () => DeleteSut().Handle(new DeleteTreasuryRoleCommand(Guid.NewGuid()) { VahedCode = Vahed }, default));

        _roles.Verify(r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRole_MissingOrAlreadyDeleted_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        await Assert.ThrowsAsync<NotFoundException>(
            () => DeleteSut().Handle(new DeleteTreasuryRoleCommand(id) { VahedCode = Vahed }, default));

        _roles.Setup(r => r.GetForUpdateAsync(id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(new TB_TR_ROLE { ID = id, ISDELETED = true });
        await Assert.ThrowsAsync<NotFoundException>(
            () => DeleteSut().Handle(new DeleteTreasuryRoleCommand(id) { VahedCode = Vahed }, default));
    }

    // ── UpsertTreasurySetting ───────────────────────────────────────────────

    private UpsertTreasurySettingCommandHandler SettingSut()
        => new(_settings.Object, _authorizer.Object, _groups.Object, _accounts.Object, _uow.Object, _user.Object);

    private static UpsertTreasurySettingCommand SettingCmd(
        Guid? beneficiaryGroup = null, Guid? payables = null, Guid? customerGroup = null, Guid? bankFee = null)
        => new(1000m, 500m, beneficiaryGroup, payables, null, null, null, customerGroup, 2000m, bankFee) { VahedCode = Vahed };

    private static AccountCodeDto Account(Guid id) => new(
        id, null, null, "1101", "بانک", null, null, null, null, null, null, null, null, false, null, null);

    private static TafsilGroupDto Group(Guid id, bool deleted = false)
        => new(id, "G1", "گروه", null, null, null, null, null, deleted);

    [Fact]
    public async Task Upsert_NotFinanceManager_Throws_AndTouchesNothing()
    {
        _authorizer.Setup(a => a.EnsureHasRoleAsync(Vahed, It.IsAny<IReadOnlyCollection<TreasuryRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryRoleRequiredException(Vahed, TreasuryRole.FinanceManager));

        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(() => SettingSut().Handle(SettingCmd(), default));

        _settings.Verify(s => s.AddAsync(It.IsAny<TB_TR_SETTING>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Upsert_NoExistingRow_CreatesAndReturnsDtoWithResolvedNames()
    {
        var group = Guid.NewGuid();
        var payables = Guid.NewGuid();
        _groups.Setup(g => g.GetByIdAsync(group, It.IsAny<CancellationToken>())).ReturnsAsync(Group(group));
        _accounts.Setup(a => a.GetByIdAsync(payables, It.IsAny<CancellationToken>())).ReturnsAsync(Account(payables));
        TB_TR_SETTING? added = null;
        _settings.Setup(s => s.AddAsync(It.IsAny<TB_TR_SETTING>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_SETTING, CancellationToken>((s, _) => added = s).Returns(Task.CompletedTask);

        var dto = await SettingSut().Handle(SettingCmd(beneficiaryGroup: group, payables: payables), default);

        Assert.NotNull(added);
        Assert.Equal(Vahed, added!.VAHEDCODE);
        Assert.Equal("caller", added.ADDUSERID);
        Assert.Equal(1000m, added.CEO_APPROVAL_THRESHOLD);
        Assert.Equal(2000m, added.DAILY_TRANSFER_LIMIT);
        Assert.Equal("G1", dto.BeneficiaryTafsilGroupCode);
        Assert.Equal("گروه", dto.BeneficiaryTafsilGroupName);
        Assert.Equal("1101", dto.PayablesAccountCode);
        Assert.Equal("بانک", dto.PayablesAccountName);
        Assert.Null(dto.VatCreditAccountCode);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upsert_ExistingRow_UpdatesInPlaceWithoutAdding()
    {
        var existing = new TB_TR_SETTING { ID = Guid.NewGuid(), VAHEDCODE = Vahed, CEO_APPROVAL_THRESHOLD = 1m };
        _settings.Setup(s => s.GetForUpdateAsync(Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var dto = await SettingSut().Handle(SettingCmd(), default);

        Assert.Equal(1000m, existing.CEO_APPROVAL_THRESHOLD);
        Assert.Equal(500m, existing.BULK_APPROVE_LIMIT);
        Assert.Equal("caller", existing.CHANGEUSERID);
        Assert.NotNull(existing.UPDATEDDATE);
        Assert.Equal(existing.ID, dto.Id);
        _settings.Verify(s => s.AddAsync(It.IsAny<TB_TR_SETTING>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Upsert_BeneficiaryGroupMissingOrDeleted_ThrowsNotFound()
    {
        var group = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(() => SettingSut().Handle(SettingCmd(beneficiaryGroup: group), default));

        _groups.Setup(g => g.GetByIdAsync(group, It.IsAny<CancellationToken>())).ReturnsAsync(Group(group, deleted: true));
        await Assert.ThrowsAsync<NotFoundException>(() => SettingSut().Handle(SettingCmd(beneficiaryGroup: group), default));
    }

    [Fact]
    public async Task Upsert_CustomerGroupMissing_ThrowsNotFound()
        => await Assert.ThrowsAsync<NotFoundException>(() => SettingSut().Handle(SettingCmd(customerGroup: Guid.NewGuid()), default));

    [Fact]
    public async Task Upsert_AccountMissing_ThrowsNotFound_AndDoesNotSave()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => SettingSut().Handle(SettingCmd(payables: Guid.NewGuid()), default));
        await Assert.ThrowsAsync<NotFoundException>(() => SettingSut().Handle(SettingCmd(bankFee: Guid.NewGuid()), default));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void UpsertValidator_Rules()
    {
        var v = new UpsertTreasurySettingCommandValidator();
        Assert.True(v.Validate(SettingCmd()).IsValid);
        Assert.False(v.Validate(SettingCmd() with { CeoApprovalThreshold = 0 }).IsValid);
        Assert.False(v.Validate(SettingCmd() with { BulkApproveLimit = -1 }).IsValid);
        Assert.False(v.Validate(SettingCmd() with { DailyTransferLimit = 0 }).IsValid);
        Assert.True(v.Validate(SettingCmd() with { DailyTransferLimit = null }).IsValid);
        Assert.False(v.Validate(SettingCmd(beneficiaryGroup: Guid.Empty)).IsValid);
        Assert.False(v.Validate(SettingCmd(payables: Guid.Empty)).IsValid);
        Assert.False(v.Validate(SettingCmd(customerGroup: Guid.Empty)).IsValid);
        Assert.False(v.Validate(SettingCmd() with { VahedCode = "" }).IsValid);
    }
}
