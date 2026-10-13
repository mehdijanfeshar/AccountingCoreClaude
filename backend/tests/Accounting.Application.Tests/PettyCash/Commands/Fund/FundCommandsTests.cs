using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.PettyCash.DeletePettyCashFund;
using Accounting.Application.Commands.PettyCash.DeletePettyCashFundReviewer;
using Accounting.Application.Commands.PettyCash.UpdatePettyCashFund;
using Accounting.Application.Commands.PettyCash.UpsertPettyCashFundReviewer;
using Accounting.Application.Commands.PettyCash.UpsertPettyCashFundTafsilis;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.Accounts;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.Fund;

/// <summary>دستورهای ویرایش/حذف تنخواه، بازبین‌ها و تفصیلی‌های تنخواه.</summary>
public class FundCommandsTests
{
    private readonly Mock<IPettyCashFundRepository> _funds = new();
    private readonly Mock<IPettyCashFundReviewerRepository> _reviewers = new();
    private readonly Mock<IAccountCodeReadRepository> _accounts = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Guid _id = Guid.NewGuid();

    public FundCommandsTests() => _user.SetupGet(u => u.UserId).Returns("editor");

    private void SaveNever() => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    private void SaveOnce() => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    private TB_PC_FUND SetupFund(bool deleted = false)
    {
        var f = new TB_PC_FUND { ID = _id, CODE = "OLD", ISDELETED = deleted };
        _funds.Setup(x => x.GetForUpdateAsync(_id, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(f);
        return f;
    }

    // ---------- UpdatePettyCashFund ----------

    private UpdatePettyCashFundCommandHandler Update() => new(_funds.Object, _accounts.Object, _uow.Object, _user.Object);

    private UpdatePettyCashFundCommand UpdateCmd(Guid? acc = null) => new(
        _id, "F2", "نام جدید", "1234567890", "علی", 2000m, 700m, 900m, 70, acc,
        PettyCashSettlementPeriod.Monthly, true, PettyCashRefundRecorder.Treasurer)
    { VahedCode = "0101" };

    [Fact]
    public async Task Update_NotFound_Throws()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Update().Handle(UpdateCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Update_Deleted_ThrowsNotFound()
    {
        SetupFund(deleted: true);
        await Assert.ThrowsAsync<NotFoundException>(() => Update().Handle(UpdateCmd(), default));
    }

    [Fact]
    public async Task Update_DuplicateCode_ExcludesOwnIdAndThrows()
    {
        SetupFund();
        _funds.Setup(f => f.ExistsByCodeAsync("F2", "0101", _id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Assert.ThrowsAsync<PettyCashFundCodeDuplicateException>(() => Update().Handle(UpdateCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Update_AccountMissing_ThrowsNotFound()
    {
        SetupFund();
        var acc = Guid.NewGuid();
        _accounts.Setup(a => a.GetByIdAsync(acc, It.IsAny<CancellationToken>())).ReturnsAsync((AccountCodeDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => Update().Handle(UpdateCmd(acc), default));
    }

    [Fact]
    public async Task Update_AccountSoftDeleted_ThrowsNotFound()
    {
        SetupFund();
        var acc = Guid.NewGuid();
        _accounts.Setup(a => a.GetByIdAsync(acc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCodeDto(acc, null, null, "1", "n", null, null, null, null, null, null, null, null, true, null, null));
        await Assert.ThrowsAsync<NotFoundException>(() => Update().Handle(UpdateCmd(acc), default));
    }

    [Fact]
    public async Task Update_Valid_OverwritesFieldsAndStampsAudit()
    {
        var f = SetupFund();
        await Update().Handle(UpdateCmd(), default);

        Assert.Equal("F2", f.CODE);
        Assert.Equal("نام جدید", f.NAME);
        Assert.Equal(2000m, f.CEILING);
        Assert.Equal(700m, f.PER_DOC_LIMIT);
        Assert.Equal(900m, f.FINANCE_MANAGER_APPROVAL_LIMIT);
        Assert.Equal(70, f.ALERT_THRESHOLD_PERCENT);
        Assert.Equal("editor", f.CHANGEUSERID);
        Assert.NotNull(f.UPDATEDDATE);
        SaveOnce();
    }

    private static UpdatePettyCashFundCommand ValidUpdate() => new(
        Guid.NewGuid(), "F1", "n", "1234567890", null, 1000m, 500m, 800m, 50, null, null, true) { VahedCode = "0101" };

    [Fact]
    public void UpdateValidator_Valid_Passes()
        => new UpdatePettyCashFundCommandValidator().TestValidate(ValidUpdate()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void UpdateValidator_PerDocAboveCeiling_Fails()
        => new UpdatePettyCashFundCommandValidator().TestValidate(ValidUpdate() with { PerDocLimit = 1500m })
            .ShouldHaveValidationErrorFor(x => x.PerDocLimit);

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void UpdateValidator_AlertOutOfRange_Fails(int pct)
        => new UpdatePettyCashFundCommandValidator().TestValidate(ValidUpdate() with { AlertThresholdPercent = pct })
            .ShouldHaveValidationErrorFor(x => x.AlertThresholdPercent);

    [Fact]
    public void UpdateValidator_EmptyIdCodeAndLongCustodian_Fail()
    {
        var r = new UpdatePettyCashFundCommandValidator().TestValidate(
            ValidUpdate() with { Id = Guid.Empty, Code = "", CustodianUserId = "12345678901", Ceiling = 0m });
        r.ShouldHaveValidationErrorFor(x => x.Id);
        r.ShouldHaveValidationErrorFor(x => x.Code);
        r.ShouldHaveValidationErrorFor(x => x.CustodianUserId);
        r.ShouldHaveValidationErrorFor(x => x.Ceiling);
    }

    [Fact]
    public void UpdateValidator_BadEnumAndMissingVahed_Fail()
    {
        var cmd = ValidUpdate() with { RefundRecorder = (PettyCashRefundRecorder)99, SettlementPeriod = (PettyCashSettlementPeriod)99 };
        cmd.VahedCode = "";
        var r = new UpdatePettyCashFundCommandValidator().TestValidate(cmd);
        r.ShouldHaveValidationErrorFor(x => x.RefundRecorder);
        r.ShouldHaveValidationErrorFor(x => x.SettlementPeriod);
        r.ShouldHaveValidationErrorFor(x => x.VahedCode);
    }

    // ---------- DeletePettyCashFund ----------

    private DeletePettyCashFundCommandHandler Delete() => new(_funds.Object, _uow.Object, _user.Object);
    private DeletePettyCashFundCommand DelCmd() => new(_id) { VahedCode = "0101" };

    [Fact]
    public async Task Delete_NotFound_Throws()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Delete().Handle(DelCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task Delete_AlreadyDeleted_IsNoOp()
    {
        SetupFund(deleted: true);
        await Delete().Handle(DelCmd(), default);
        SaveNever();
    }

    [Fact]
    public async Task Delete_HasActiveExpenseDocs_Throws()
    {
        var f = SetupFund();
        _funds.Setup(x => x.HasActiveExpenseDocsAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Assert.ThrowsAsync<PettyCashFundHasExpenseDocsException>(() => Delete().Handle(DelCmd(), default));
        Assert.False(f.ISDELETED);
        SaveNever();
    }

    [Fact]
    public async Task Delete_Valid_SoftDeletes()
    {
        var f = SetupFund();
        await Delete().Handle(DelCmd(), default);
        Assert.True(f.ISDELETED);
        Assert.Equal("editor", f.CHANGEUSERID);
        SaveOnce();
    }

    [Fact]
    public void DeleteValidator_EmptyId_Fails()
        => Assert.False(new DeletePettyCashFundCommandValidator().TestValidate(new DeletePettyCashFundCommand(Guid.Empty) { VahedCode = "0101" }).IsValid);

    // ---------- UpsertPettyCashFundReviewer ----------

    private UpsertPettyCashFundReviewerCommandHandler Upsert() => new(_funds.Object, _reviewers.Object, _uow.Object, _user.Object);
    private UpsertPettyCashFundReviewerCommand UpsertCmd() => new(_id, "1111111111", "بازبین", PettyCashRole.FinanceManager) { VahedCode = "0101" };

    [Fact]
    public async Task UpsertReviewer_FundMissing_Throws()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Upsert().Handle(UpsertCmd(), default));
        SaveNever();
    }

    [Fact]
    public async Task UpsertReviewer_New_AddsWithVahedAndUser()
    {
        SetupFund();
        TB_PC_REVIEWER? added = null;
        _reviewers.Setup(r => r.AddAsync(It.IsAny<TB_PC_REVIEWER>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_REVIEWER, CancellationToken>((x, _) => added = x).Returns(Task.CompletedTask);

        var id = await Upsert().Handle(UpsertCmd(), default);

        Assert.NotNull(added);
        Assert.Equal(added!.ID, id);
        Assert.Equal("0101", added.VAHEDCODE);
        Assert.Equal("editor", added.ADDUSERID);
        Assert.Equal(PettyCashRole.FinanceManager, added.ROLE);
        Assert.False(added.ISDELETED);
        SaveOnce();
    }

    [Fact]
    public async Task UpsertReviewer_Existing_ReactivatesAndRenames()
    {
        SetupFund();
        var existing = new TB_PC_REVIEWER { ID = Guid.NewGuid(), ISDELETED = true, REVIEWER_NAME = "قدیمی" };
        _reviewers.Setup(r => r.GetByFundUserAndRoleAsync(_id, "1111111111", PettyCashRole.FinanceManager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var id = await Upsert().Handle(UpsertCmd(), default);

        Assert.Equal(existing.ID, id);
        Assert.False(existing.ISDELETED);
        Assert.Equal("بازبین", existing.REVIEWER_NAME);
        Assert.Equal("editor", existing.CHANGEUSERID);
        _reviewers.Verify(r => r.AddAsync(It.IsAny<TB_PC_REVIEWER>(), It.IsAny<CancellationToken>()), Times.Never);
        SaveOnce();
    }

    [Fact]
    public void UpsertReviewerValidator_Rules()
    {
        var v = new UpsertPettyCashFundReviewerCommandValidator();
        v.TestValidate(UpsertCmd()).ShouldNotHaveAnyValidationErrors();
        v.TestValidate(UpsertCmd() with { FundId = Guid.Empty }).ShouldHaveValidationErrorFor(x => x.FundId);
        v.TestValidate(UpsertCmd() with { ReviewerUserId = "" }).ShouldHaveValidationErrorFor(x => x.ReviewerUserId);
        v.TestValidate(UpsertCmd() with { ReviewerUserId = "12345678901" }).ShouldHaveValidationErrorFor(x => x.ReviewerUserId);
        v.TestValidate(UpsertCmd() with { Role = (PettyCashRole)99 }).ShouldHaveValidationErrorFor(x => x.Role);
    }

    // ---------- DeletePettyCashFundReviewer ----------

    private DeletePettyCashFundReviewerCommandHandler DeleteReviewer() => new(_reviewers.Object, _uow.Object, _user.Object);
    private readonly Guid _revId = Guid.NewGuid();

    private TB_PC_REVIEWER SetupReviewer(Guid fundId, bool deleted = false)
    {
        var r = new TB_PC_REVIEWER { ID = _revId, FUND_ID = fundId, ISDELETED = deleted };
        _reviewers.Setup(x => x.GetForUpdateAsync(_revId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(r);
        return r;
    }

    [Fact]
    public async Task DeleteReviewer_Missing_Throws()
        => await Assert.ThrowsAsync<NotFoundException>(
            () => DeleteReviewer().Handle(new DeletePettyCashFundReviewerCommand(_id, _revId) { VahedCode = "0101" }, default));

    [Fact]
    public async Task DeleteReviewer_BelongsToOtherFund_ThrowsNotFound()
    {
        SetupReviewer(Guid.NewGuid());
        await Assert.ThrowsAsync<NotFoundException>(
            () => DeleteReviewer().Handle(new DeletePettyCashFundReviewerCommand(_id, _revId) { VahedCode = "0101" }, default));
        SaveNever();
    }

    [Fact]
    public async Task DeleteReviewer_AlreadyDeleted_IsNoOp()
    {
        SetupReviewer(_id, deleted: true);
        await DeleteReviewer().Handle(new DeletePettyCashFundReviewerCommand(_id, _revId) { VahedCode = "0101" }, default);
        SaveNever();
    }

    [Fact]
    public async Task DeleteReviewer_Valid_SoftDeletes()
    {
        var r = SetupReviewer(_id);
        await DeleteReviewer().Handle(new DeletePettyCashFundReviewerCommand(_id, _revId) { VahedCode = "0101" }, default);
        Assert.True(r.ISDELETED);
        Assert.Equal("editor", r.CHANGEUSERID);
        SaveOnce();
    }

    // ---------- UpsertPettyCashFundTafsilis ----------

    private UpsertPettyCashFundTafsilisCommandHandler Tafsilis() => new(_funds.Object, _uow.Object, _user.Object);

    [Fact]
    public async Task Tafsilis_FundMissing_Throws()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => Tafsilis().Handle(new UpsertPettyCashFundTafsilisCommand(_id, null) { VahedCode = "0101" }, default));
        SaveNever();
    }

    [Fact]
    public async Task Tafsilis_Reconcile_KeepsSurvivorDeletesStaleAddsNew()
    {
        SetupFund();
        Guid t1 = Guid.NewGuid(), l1 = Guid.NewGuid(), t2 = Guid.NewGuid(), l2 = Guid.NewGuid(), t3 = Guid.NewGuid(), l3 = Guid.NewGuid();
        var keep = new TB_PC_FUND_LINK_TAFSILI { TAFSILI_ID = t1, LEVEL_ID = l1 };
        var stale = new TB_PC_FUND_LINK_TAFSILI { TAFSILI_ID = t2, LEVEL_ID = l2 };
        _funds.Setup(f => f.GetActiveFundTafsiliLinksAsync(_id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_PC_FUND_LINK_TAFSILI> { keep, stale });
        var added = new List<TB_PC_FUND_LINK_TAFSILI>();
        _funds.Setup(f => f.AddFundTafsiliLinkAsync(It.IsAny<TB_PC_FUND_LINK_TAFSILI>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_FUND_LINK_TAFSILI, CancellationToken>((x, _) => added.Add(x)).Returns(Task.CompletedTask);

        await Tafsilis().Handle(new UpsertPettyCashFundTafsilisCommand(_id, new[]
        {
            new PettyCashFundTafsiliLinkInput(t1, l1), new PettyCashFundTafsiliLinkInput(t3, l3),
        })
        { VahedCode = "0101" }, default);

        Assert.False(keep.ISDELETED);
        Assert.Null(keep.CHANGEUSERID);
        Assert.True(stale.ISDELETED);
        Assert.Equal("editor", stale.CHANGEUSERID);
        var n = Assert.Single(added);
        Assert.Equal(t3, n.TAFSILI_ID);
        Assert.Equal("0101", n.VAHEDCODE);
        Assert.Equal(_id, n.FUND_ID);
        SaveOnce();
    }

    [Fact]
    public async Task Tafsilis_NullSet_SoftDeletesAll()
    {
        SetupFund();
        var a = new TB_PC_FUND_LINK_TAFSILI { TAFSILI_ID = Guid.NewGuid(), LEVEL_ID = Guid.NewGuid() };
        _funds.Setup(f => f.GetActiveFundTafsiliLinksAsync(_id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_PC_FUND_LINK_TAFSILI> { a });

        await Tafsilis().Handle(new UpsertPettyCashFundTafsilisCommand(_id, null) { VahedCode = "0101" }, default);

        Assert.True(a.ISDELETED);
        SaveOnce();
    }

    [Fact]
    public void TafsilisValidator_Rules()
    {
        var v = new UpsertPettyCashFundTafsilisCommandValidator();
        v.TestValidate(new UpsertPettyCashFundTafsilisCommand(Guid.Empty, null) { VahedCode = "0101" })
            .ShouldHaveValidationErrorFor(x => x.FundId);
        Assert.False(v.TestValidate(new UpsertPettyCashFundTafsilisCommand(_id, new[] { new PettyCashFundTafsiliLinkInput(Guid.Empty, Guid.NewGuid()) }) { VahedCode = "0101" }).IsValid);
        v.TestValidate(new UpsertPettyCashFundTafsilisCommand(_id, new[] { new PettyCashFundTafsiliLinkInput(Guid.NewGuid(), Guid.NewGuid()) }) { VahedCode = "0101" })
            .ShouldNotHaveAnyValidationErrors();
    }
}
