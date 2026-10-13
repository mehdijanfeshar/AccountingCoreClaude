using Accounting.Application.Commands.Treasury.AddBankStatementLine;
using Accounting.Application.Commands.Treasury.CloseBankStatement;
using Accounting.Application.Commands.Treasury.CreateBankStatement;
using Accounting.Application.Commands.Treasury.DeleteBankStatement;
using Accounting.Application.Commands.Treasury.DeleteBankStatementLine;
using Accounting.Application.Commands.Treasury.ReopenBankStatement;
using Accounting.Application.Commands.Treasury.UpdateBankStatement;
using Accounting.Application.Commands.Treasury.UpdateBankStatementLine;
using Accounting.Application.Commands.Treasury.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.BankAccounts;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.Treasury.Commands.BankStatements;

/// <summary>
/// CRUD صورت‌حساب بانکی و ردیف‌هایش: فقط خزانه‌دار/حسابدار ارشد، فقط صورت‌حساب «باز»،
/// ردیف فقط وقتی «نامنطبق» است قابل ویرایش/حذف.
/// </summary>
public sealed class BankStatementCrudHandlerTests
{
    private const string Vahed = "V1";
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid LineId = Guid.NewGuid();
    private static readonly Guid Bank = Guid.NewGuid();

    private readonly Mock<ITreasuryBankStatementRepository> _statements = new();
    private readonly Mock<ITreasuryBankStatementLineRepository> _lines = new();
    private readonly Mock<IBankAccountReadRepository> _banks = new();
    private readonly Mock<ITreasuryRoleAuthorizer> _roles = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public BankStatementCrudHandlerTests()
    {
        _user.SetupGet(u => u.UserId).Returns("acc1");
        _banks.Setup(b => b.GetByIdAsync(Bank, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(BankDto());
    }

    private static BankAccountDto BankDto() => new(
        Bank, "1", "h", null, null, null, null, null, null, Guid.NewGuid(), Vahed, null, null, null, null, null, false,
        new List<BankAccountTafsiliLinkDto>());

    private TB_TR_BANK_STATEMENT Existing(BankStatementState state)
    {
        var s = new TB_TR_BANK_STATEMENT { ID = Id, STATE = state, YEAR = "1405", VAHEDCODE = Vahed };
        _statements.Setup(r => r.GetForUpdateAsync(Id, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync(s);
        return s;
    }

    private void DenyRole()
        => _roles.Setup(r => r.EnsureHasRoleAsync(Vahed, It.IsAny<IReadOnlyCollection<TreasuryRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TreasuryRoleRequiredException(Vahed, TreasuryRole.Treasurer));

    private void VerifyEditorRolesChecked()
        => _roles.Verify(r => r.EnsureHasRoleAsync(Vahed,
            It.Is<IReadOnlyCollection<TreasuryRole>>(c => c.Contains(TreasuryRole.Treasurer) && c.Contains(TreasuryRole.SeniorAccountant) && c.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);

    // ── Create ──────────────────────────────────────────────────────────────

    private CreateBankStatementCommand CreateCmd() => new(Bank, "14050101", "14050130", 5000m, "d", "1405") { VahedCode = Vahed };

    private CreateBankStatementCommandHandler CreateSut() => new(_statements.Object, _banks.Object, _roles.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task Create_Valid_StagesOpenManualStatement()
    {
        _statements.Setup(s => s.GetNextCodeAsync(Vahed, "1405", It.IsAny<CancellationToken>())).ReturnsAsync(4);
        TB_TR_BANK_STATEMENT? added = null;
        _statements.Setup(s => s.AddAsync(It.IsAny<TB_TR_BANK_STATEMENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_BANK_STATEMENT, CancellationToken>((s, _) => added = s).Returns(Task.CompletedTask);

        var id = await CreateSut().Handle(CreateCmd(), default);

        Assert.Equal(id, added!.ID);
        Assert.Equal("BST-000004", added.CODE);
        Assert.Equal(BankStatementState.Open, added.STATE);
        Assert.Equal(BankStatementSource.Manual, added.SOURCE);
        Assert.Equal(Vahed, added.VAHEDCODE);
        Assert.Equal("acc1", added.ADDUSERID);
        VerifyEditorRolesChecked();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_RoleDenied_Throws_NothingStaged()
    {
        DenyRole();

        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(() => CreateSut().Handle(CreateCmd(), default));

        _statements.Verify(s => s.AddAsync(It.IsAny<TB_TR_BANK_STATEMENT>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_BankMissing_ThrowsNotFound()
    {
        _banks.Setup(b => b.GetByIdAsync(Bank, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateSut().Handle(CreateCmd(), default));
    }

    // ── Update ──────────────────────────────────────────────────────────────

    private UpdateBankStatementCommand UpdateCmd() => new(Id, Bank, "14050201", "14050230", 7000m, "n") { VahedCode = Vahed };

    private UpdateBankStatementCommandHandler UpdateSut() => new(_statements.Object, _banks.Object, _roles.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task Update_OpenStatement_AppliesFields()
    {
        var s = Existing(BankStatementState.Open);

        await UpdateSut().Handle(UpdateCmd(), default);

        Assert.Equal("14050201", s.FROM_DATE);
        Assert.Equal(7000m, s.CLOSING_BALANCE);
        Assert.Equal("acc1", s.CHANGEUSERID);
        VerifyEditorRolesChecked();
    }

    [Fact]
    public async Task Update_ClosedStatement_ThrowsStateConflict()
    {
        Existing(BankStatementState.Closed);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(() => UpdateSut().Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_RoleDenied_ThrowsBeforeLookup()
    {
        DenyRole();

        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(() => UpdateSut().Handle(UpdateCmd(), default));

        _statements.Verify(s => s.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_NotFoundOrDeleted_Throws()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => UpdateSut().Handle(UpdateCmd(), default));

        Existing(BankStatementState.Open).ISDELETED = true;
        await Assert.ThrowsAsync<NotFoundException>(() => UpdateSut().Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_BankMissing_ThrowsNotFound()
    {
        Existing(BankStatementState.Open);
        _banks.Setup(b => b.GetByIdAsync(Bank, Vahed, It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => UpdateSut().Handle(UpdateCmd(), default));
    }

    // ── Delete ──────────────────────────────────────────────────────────────

    private DeleteBankStatementCommandHandler DeleteSut() => new(_statements.Object, _lines.Object, _roles.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task Delete_Open_SoftDeletesStatementAndCascadesToLines()
    {
        var s = Existing(BankStatementState.Open);

        await DeleteSut().Handle(new DeleteBankStatementCommand(Id) { VahedCode = Vahed }, default);

        Assert.True(s.ISDELETED);
        _lines.Verify(l => l.SoftDeleteByStatementAsync(Id, "acc1", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Closed_Throws_NoCascade()
    {
        var s = Existing(BankStatementState.Closed);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(
            () => DeleteSut().Handle(new DeleteBankStatementCommand(Id) { VahedCode = Vahed }, default));

        Assert.False(s.ISDELETED);
        _lines.Verify(l => l.SoftDeleteByStatementAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(
            () => DeleteSut().Handle(new DeleteBankStatementCommand(Id) { VahedCode = Vahed }, default));

    // ── Close / Reopen ──────────────────────────────────────────────────────

    [Fact]
    public async Task Close_Open_BecomesClosed()
    {
        var s = Existing(BankStatementState.Open);

        await new CloseBankStatementCommandHandler(_statements.Object, _roles.Object, _uow.Object, _user.Object)
            .Handle(new CloseBankStatementCommand(Id) { VahedCode = Vahed }, default);

        Assert.Equal(BankStatementState.Closed, s.STATE);
        VerifyEditorRolesChecked();
    }

    [Fact]
    public async Task Close_AlreadyClosed_Throws()
    {
        Existing(BankStatementState.Closed);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(
            () => new CloseBankStatementCommandHandler(_statements.Object, _roles.Object, _uow.Object, _user.Object)
                .Handle(new CloseBankStatementCommand(Id) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Close_RoleDenied_Throws()
    {
        DenyRole();

        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(
            () => new CloseBankStatementCommandHandler(_statements.Object, _roles.Object, _uow.Object, _user.Object)
                .Handle(new CloseBankStatementCommand(Id) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Reopen_Closed_BecomesOpen()
    {
        var s = Existing(BankStatementState.Closed);

        await new ReopenBankStatementCommandHandler(_statements.Object, _roles.Object, _uow.Object, _user.Object)
            .Handle(new ReopenBankStatementCommand(Id) { VahedCode = Vahed }, default);

        Assert.Equal(BankStatementState.Open, s.STATE);
    }

    [Fact]
    public async Task Reopen_AlreadyOpen_Throws()
    {
        Existing(BankStatementState.Open);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(
            () => new ReopenBankStatementCommandHandler(_statements.Object, _roles.Object, _uow.Object, _user.Object)
                .Handle(new ReopenBankStatementCommand(Id) { VahedCode = Vahed }, default));
    }

    [Fact]
    public async Task Reopen_NotFound_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(
            () => new ReopenBankStatementCommandHandler(_statements.Object, _roles.Object, _uow.Object, _user.Object)
                .Handle(new ReopenBankStatementCommand(Id) { VahedCode = Vahed }, default));

    // ── Lines ───────────────────────────────────────────────────────────────

    private TB_TR_BANK_STATEMENT_LINE ExistingLine(BankStatementLineMatchState state)
    {
        var l = new TB_TR_BANK_STATEMENT_LINE { ID = LineId, STATEMENT_ID = Id, MATCH_STATE = state };
        _lines.Setup(r => r.GetForUpdateAsync(LineId, Id, It.IsAny<CancellationToken>())).ReturnsAsync(l);
        return l;
    }

    private AddBankStatementLineCommand AddCmd(decimal withdrawal = 0m, decimal deposit = 100m)
        => new(Id, "14050105", "REF", "d", withdrawal, deposit, 1000m) { VahedCode = Vahed };

    private UpdateBankStatementLineCommand UpdLineCmd(decimal withdrawal = 50m, decimal deposit = 0m)
        => new(Id, LineId, "14050106", "R2", "d2", withdrawal, deposit, 900m) { VahedCode = Vahed };

    private AddBankStatementLineCommandHandler AddSut() => new(_statements.Object, _lines.Object, _roles.Object, _uow.Object, _user.Object);
    private UpdateBankStatementLineCommandHandler UpdLineSut() => new(_statements.Object, _lines.Object, _roles.Object, _uow.Object, _user.Object);
    private DeleteBankStatementLineCommandHandler DelLineSut() => new(_statements.Object, _lines.Object, _roles.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task AddLine_Open_StagesUnmatchedLineInheritingYear()
    {
        Existing(BankStatementState.Open);
        TB_TR_BANK_STATEMENT_LINE? added = null;
        _lines.Setup(l => l.AddAsync(It.IsAny<TB_TR_BANK_STATEMENT_LINE>(), It.IsAny<CancellationToken>()))
            .Callback<TB_TR_BANK_STATEMENT_LINE, CancellationToken>((l, _) => added = l).Returns(Task.CompletedTask);

        var id = await AddSut().Handle(AddCmd(), default);

        Assert.Equal(id, added!.ID);
        Assert.Equal(Id, added.STATEMENT_ID);
        Assert.Equal(BankStatementLineMatchState.Unmatched, added.MATCH_STATE);
        Assert.Equal("1405", added.YEAR);
        Assert.Equal(100m, added.DEPOSIT);
        Assert.Equal(Vahed, added.VAHEDCODE);
    }

    [Fact]
    public async Task AddLine_ClosedStatement_Throws()
    {
        Existing(BankStatementState.Closed);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(() => AddSut().Handle(AddCmd(), default));
    }

    [Fact]
    public async Task AddLine_StatementMissing_ThrowsNotFound()
        => await Assert.ThrowsAsync<NotFoundException>(() => AddSut().Handle(AddCmd(), default));

    [Fact]
    public async Task AddLine_RoleDenied_Throws()
    {
        DenyRole();

        await Assert.ThrowsAsync<TreasuryRoleRequiredException>(() => AddSut().Handle(AddCmd(), default));
    }

    [Fact]
    public async Task UpdateLine_Unmatched_AppliesFields()
    {
        Existing(BankStatementState.Open);
        var l = ExistingLine(BankStatementLineMatchState.Unmatched);

        await UpdLineSut().Handle(UpdLineCmd(), default);

        Assert.Equal("14050106", l.LINE_DATE);
        Assert.Equal(50m, l.WITHDRAWAL);
        Assert.Equal("acc1", l.CHANGEUSERID);
    }

    [Theory]
    [InlineData(BankStatementLineMatchState.AutoMatched)]
    [InlineData(BankStatementLineMatchState.ManualMatched)]
    [InlineData(BankStatementLineMatchState.Resolved)]
    public async Task UpdateLine_NotUnmatched_Throws(BankStatementLineMatchState state)
    {
        Existing(BankStatementState.Open);
        ExistingLine(state);

        await Assert.ThrowsAsync<TreasuryBankStatementLineStateConflictException>(() => UpdLineSut().Handle(UpdLineCmd(), default));
    }

    [Fact]
    public async Task UpdateLine_LineMissingOrDeleted_Throws()
    {
        Existing(BankStatementState.Open);
        await Assert.ThrowsAsync<NotFoundException>(() => UpdLineSut().Handle(UpdLineCmd(), default));

        ExistingLine(BankStatementLineMatchState.Unmatched).ISDELETED = true;
        await Assert.ThrowsAsync<NotFoundException>(() => UpdLineSut().Handle(UpdLineCmd(), default));
    }

    [Fact]
    public async Task UpdateLine_ClosedStatement_Throws()
    {
        Existing(BankStatementState.Closed);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(() => UpdLineSut().Handle(UpdLineCmd(), default));
    }

    [Fact]
    public async Task DeleteLine_Unmatched_SoftDeletes()
    {
        Existing(BankStatementState.Open);
        var l = ExistingLine(BankStatementLineMatchState.Unmatched);

        await DelLineSut().Handle(new DeleteBankStatementLineCommand(Id, LineId) { VahedCode = Vahed }, default);

        Assert.True(l.ISDELETED);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLine_Matched_Throws_NotDeleted()
    {
        Existing(BankStatementState.Open);
        var l = ExistingLine(BankStatementLineMatchState.ManualMatched);

        await Assert.ThrowsAsync<TreasuryBankStatementLineStateConflictException>(
            () => DelLineSut().Handle(new DeleteBankStatementLineCommand(Id, LineId) { VahedCode = Vahed }, default));

        Assert.False(l.ISDELETED);
    }

    [Fact]
    public async Task DeleteLine_StatementClosed_Throws()
    {
        Existing(BankStatementState.Closed);

        await Assert.ThrowsAsync<TreasuryBankStatementStateConflictException>(
            () => DelLineSut().Handle(new DeleteBankStatementLineCommand(Id, LineId) { VahedCode = Vahed }, default));
    }

    // ── Validators ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateValidator_Rules()
    {
        var v = new CreateBankStatementCommandValidator();
        Assert.True(v.Validate(CreateCmd()).IsValid);
        Assert.True(v.Validate(CreateCmd() with { FromDate = "14050130", ToDate = "14050130" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { FromDate = "14050131", ToDate = "14050101" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { FromDate = "bad" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { BankAccountId = Guid.Empty }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { Year = "14" }).IsValid);
        Assert.False(v.Validate(CreateCmd() with { Description = new string('x', 1001) }).IsValid);
    }

    [Fact]
    public void UpdateValidator_Rules()
    {
        var v = new UpdateBankStatementCommandValidator();
        Assert.True(v.Validate(UpdateCmd()).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { FromDate = "14060101", ToDate = "14050101" }).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { Id = Guid.Empty }).IsValid);
        Assert.False(v.Validate(UpdateCmd() with { ToDate = "" }).IsValid);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(10, 10, false)]
    [InlineData(10, 0, true)]
    [InlineData(0, 10, true)]
    [InlineData(-1, 0, false)]
    public void LineValidators_ExactlyOneOfWithdrawalOrDeposit(int withdrawal, int deposit, bool expectedValid)
    {
        Assert.Equal(expectedValid, new AddBankStatementLineCommandValidator().Validate(AddCmd(withdrawal, deposit)).IsValid);
        Assert.Equal(expectedValid, new UpdateBankStatementLineCommandValidator().Validate(UpdLineCmd(withdrawal, deposit)).IsValid);
    }

    [Fact]
    public void LineValidators_DateAndIdRules()
    {
        Assert.False(new AddBankStatementLineCommandValidator().Validate(AddCmd() with { LineDate = "1405" }).IsValid);
        Assert.False(new AddBankStatementLineCommandValidator().Validate(AddCmd() with { StatementId = Guid.Empty }).IsValid);
        Assert.False(new UpdateBankStatementLineCommandValidator().Validate(UpdLineCmd() with { LineId = Guid.Empty }).IsValid);
        Assert.False(new AddBankStatementLineCommandValidator().Validate(AddCmd() with { BankReference = new string('x', 101) }).IsValid);
    }
}
