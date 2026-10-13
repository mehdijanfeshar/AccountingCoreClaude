using Accounting.Application.Commands.PettyCash.CreatePettyCashFund;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.Accounts;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.CreatePettyCashFund;

public class CreatePettyCashFundTests
{
    private readonly Mock<IPettyCashFundRepository> _funds = new();
    private readonly Mock<IAccountCodeReadRepository> _accounts = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();

    public CreatePettyCashFundTests() => _user.SetupGet(u => u.UserId).Returns("creator");

    private CreatePettyCashFundCommandHandler Sut() => new(_funds.Object, _accounts.Object, _uow.Object, _user.Object);

    private static CreatePettyCashFundCommand Cmd(Guid? accountCodeId = null) => new(
        "F1", "تنخواه مرکز", "1234567890", "علی", 1000m, 500m, 800m, 80, accountCodeId,
        PettyCashSettlementPeriod.Monthly, true, PettyCashRefundRecorder.Treasurer)
    { VahedCode = "0101" };

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsDuplicate()
    {
        _funds.Setup(f => f.ExistsByCodeAsync("F1", "0101", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<PettyCashFundCodeDuplicateException>(() => Sut().Handle(Cmd(), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AccountCodeMissing_ThrowsNotFound()
    {
        var acc = Guid.NewGuid();
        _accounts.Setup(a => a.GetByIdAsync(acc, It.IsAny<CancellationToken>())).ReturnsAsync((AccountCodeDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().Handle(Cmd(acc), default));
    }

    [Fact]
    public async Task Handle_AccountCodeSoftDeleted_ThrowsNotFound()
    {
        var acc = Guid.NewGuid();
        _accounts.Setup(a => a.GetByIdAsync(acc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCodeDto(acc, null, null, "1", "n", null, null, null, null, null, null, null, null, true, null, null));

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().Handle(Cmd(acc), default));
    }

    [Fact]
    public async Task Handle_Valid_StampsVahedAndUserAndSaves()
    {
        TB_PC_FUND? saved = null;
        _funds.Setup(f => f.AddAsync(It.IsAny<TB_PC_FUND>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_FUND, CancellationToken>((x, _) => saved = x).Returns(Task.CompletedTask);

        var id = await Sut().Handle(Cmd(), default);

        Assert.Equal(id, saved!.ID);
        Assert.Equal("0101", saved.VAHEDCODE);
        Assert.Equal("creator", saved.ADDUSERID);
        Assert.Equal(1000m, saved.CEILING);
        Assert.Equal(500m, saved.PER_DOC_LIMIT);
        Assert.Equal(800m, saved.FINANCE_MANAGER_APPROVAL_LIMIT);
        Assert.False(saved.ISDELETED);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidWithActiveAccountCode_LinksAccount()
    {
        var acc = Guid.NewGuid();
        _accounts.Setup(a => a.GetByIdAsync(acc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountCodeDto(acc, null, null, "1", "n", null, null, null, null, null, null, null, null, false, null, null));
        TB_PC_FUND? saved = null;
        _funds.Setup(f => f.AddAsync(It.IsAny<TB_PC_FUND>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_FUND, CancellationToken>((x, _) => saved = x).Returns(Task.CompletedTask);

        await Sut().Handle(Cmd(acc), default);

        Assert.Equal(acc, saved!.ACCOUNTCODE_ID);
    }

    // ---------- Validator ----------

    private readonly CreatePettyCashFundCommandValidator _val = new();

    [Fact]
    public void Validator_ValidCommand_Passes() => _val.TestValidate(Cmd()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validator_PerDocLimitAboveCeiling_Fails()
        => _val.TestValidate(Cmd() with { PerDocLimit = 1001m }).ShouldHaveValidationErrorFor(x => x.PerDocLimit);

    [Fact]
    public void Validator_PerDocLimitEqualCeiling_Passes()
        => _val.TestValidate(Cmd() with { PerDocLimit = 1000m }).ShouldNotHaveValidationErrorFor(x => x.PerDocLimit);

    [Fact]
    public void Validator_NonPositiveAmounts_Fail()
    {
        var r = _val.TestValidate(Cmd() with { Ceiling = 0m, FinanceManagerApprovalLimit = 0m });
        r.ShouldHaveValidationErrorFor(x => x.Ceiling);
        r.ShouldHaveValidationErrorFor(x => x.FinanceManagerApprovalLimit);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validator_AlertThresholdOutOfRange_Fails(int pct)
        => _val.TestValidate(Cmd() with { AlertThresholdPercent = pct }).ShouldHaveValidationErrorFor(x => x.AlertThresholdPercent);

    [Fact]
    public void Validator_AlertThresholdNull_Passes()
        => _val.TestValidate(Cmd() with { AlertThresholdPercent = null }).ShouldNotHaveValidationErrorFor(x => x.AlertThresholdPercent);

    [Fact]
    public void Validator_CustodianTooLong_Fails()
        => _val.TestValidate(Cmd() with { CustodianUserId = "12345678901" }).ShouldHaveValidationErrorFor(x => x.CustodianUserId);

    [Fact]
    public void Validator_EmptyCodeNameVahed_Fail()
    {
        var c = Cmd() with { Code = "", Name = "" };
        c.VahedCode = "";
        var r = _val.TestValidate(c);
        r.ShouldHaveValidationErrorFor(x => x.Code);
        r.ShouldHaveValidationErrorFor(x => x.Name);
        r.ShouldHaveValidationErrorFor(x => x.VahedCode);
    }

    [Fact]
    public void Validator_InvalidEnum_Fails()
        => _val.TestValidate(Cmd() with { RefundRecorder = (PettyCashRefundRecorder)99 }).ShouldHaveValidationErrorFor(x => x.RefundRecorder);
}
