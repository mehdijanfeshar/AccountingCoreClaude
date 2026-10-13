using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.PettyCash.CreatePettyCashReplenishment;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.BankAccounts;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.Replenishment;

public class CreatePettyCashReplenishmentTests
{
    private readonly Mock<IPettyCashFundRepository> _funds = new();
    private readonly Mock<IBankAccountReadRepository> _banks = new();
    private readonly Mock<IPettyCashExpenseDocRepository> _docs = new();
    private readonly Mock<IChargeAndCostRepository> _charge = new();
    private readonly Mock<IPettyCashReplenishmentRepository> _repl = new();
    private readonly Mock<IPettyCashReplenishmentAuthorizer> _auth = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Guid _fundId = Guid.NewGuid();
    private readonly Guid _bankId = Guid.NewGuid();

    public CreatePettyCashReplenishmentTests() => _user.SetupGet(u => u.UserId).Returns("treasurer");

    private CreatePettyCashReplenishmentCommandHandler Sut()
        => new(_funds.Object, _banks.Object, _docs.Object, _charge.Object, _repl.Object, _auth.Object, _uow.Object, _user.Object);

    private CreatePettyCashReplenishmentCommand Cmd(bool submit = false)
        => new(_fundId, _bankId, PettyCashPaymentMethod.BankTransferToCustodian, "14040510", "1404", "یادداشت", submit) { VahedCode = "0101" };

    private void SetupFund(decimal ceiling = 10_000m)
        => _funds.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TB_PC_FUND { ID = _fundId, CEILING = ceiling });

    private void SetupBank()
        => _banks.Setup(b => b.GetByIdAsync(_bankId, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankAccountDto(_bankId, "1", "h", null, null, null, null, null, null, null, "0101", null, null, null, null, null, false,
                new List<BankAccountTafsiliLinkDto>()));

    private void SetupCandidates(params decimal[] amounts)
        => _docs.Setup(d => d.GetApprovedUnlinkedByFundAsync(_fundId, "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(amounts.Select(a => new PettyCashReplenishableDocRow(Guid.NewGuid(), Guid.NewGuid(), a, null)).ToList());

    [Fact]
    public async Task Handle_FundMissing_ThrowsNotFound()
    {
        _funds.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((TB_PC_FUND?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Handle_RoleMissing_Propagates()
    {
        SetupFund();
        _auth.Setup(a => a.EnsureHasRoleAsync(_fundId, It.IsAny<IReadOnlyCollection<PettyCashRole>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PettyCashReplenishmentRoleRequiredException(_fundId, new[] { PettyCashRole.Treasurer }));

        await Assert.ThrowsAsync<PettyCashReplenishmentRoleRequiredException>(() => Sut().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Handle_BankAccountMissing_ThrowsNotFound()
    {
        SetupFund();
        _banks.Setup(b => b.GetByIdAsync(_bankId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((BankAccountDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Handle_NoApprovedDocs_ThrowsNoDocuments()
    {
        SetupFund();
        SetupBank();
        SetupCandidates();

        await Assert.ThrowsAsync<PettyCashNoDocumentsToReplenishException>(() => Sut().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Handle_TotalExceedsCeiling_Throws()
    {
        SetupFund(ceiling: 100m);
        SetupBank();
        SetupCandidates(60m, 50m);

        await Assert.ThrowsAsync<PettyCashReplenishmentExceedsCeilingException>(() => Sut().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Handle_DocAlreadyLinked_ThrowsAlreadyReplenished()
    {
        SetupFund();
        SetupBank();
        SetupCandidates(100m);
        _charge.Setup(c => c.ExistsActiveLinkForCostAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<PettyCashDocAlreadyReplenishedException>(() => Sut().Handle(Cmd(), default));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false, PettyCashReplenishmentState.Draft)]
    [InlineData(true, PettyCashReplenishmentState.PendingFinanceManager)]
    public async Task Handle_Valid_CreatesHeadReplenishmentAndLinks(bool submit, PettyCashReplenishmentState expected)
    {
        SetupFund();
        SetupBank();
        SetupCandidates(100m, 250m);
        _charge.Setup(c => c.GetNextCodeAsync("0101", "1404", ChargeAndCostType.Charge, It.IsAny<CancellationToken>())).ReturnsAsync(7);
        TB_CHARGEANDCOST_HEAD? head = null;
        TB_PC_REPLENISHMENT? repl = null;
        _charge.Setup(c => c.AddHeadAsync(It.IsAny<TB_CHARGEANDCOST_HEAD>(), It.IsAny<CancellationToken>()))
            .Callback<TB_CHARGEANDCOST_HEAD, CancellationToken>((h, _) => head = h).Returns(Task.CompletedTask);
        _repl.Setup(r => r.AddAsync(It.IsAny<TB_PC_REPLENISHMENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_REPLENISHMENT, CancellationToken>((r, _) => repl = r).Returns(Task.CompletedTask);

        var id = await Sut().Handle(Cmd(submit), default);

        Assert.Equal(id, repl!.ID);
        Assert.Equal(expected, repl.STATE);
        Assert.Equal(350m, repl.TOTAL_AMOUNT);
        Assert.Equal("RCH-00007", repl.CODE);
        Assert.Equal("00007", head!.CHARGEANDCOST_CODE);
        Assert.Equal(_bankId, head.ACCOUNT_ID);
        Assert.Equal("0101", repl.VAHEDCODE);
        Assert.Equal("1404", repl.YEAR);
        Assert.Equal("treasurer", repl.ADDUSERID);
        _charge.Verify(c => c.AddLinkAsync(It.Is<TB_CHARGE_LINK_COST>(l => l.CHARGE_ID == head.ID && l.VAHEDCODE == "0101"),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Valid_RequestsFinanceManagerOrTreasurerRole()
    {
        SetupFund();
        SetupBank();
        SetupCandidates(10m);

        await Sut().Handle(Cmd(), default);

        _auth.Verify(a => a.EnsureHasRoleAsync(_fundId,
            It.Is<IReadOnlyCollection<PettyCashRole>>(r => r.Count == 2 && r.Contains(PettyCashRole.FinanceManager) && r.Contains(PettyCashRole.Treasurer)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Validator ----------

    private readonly CreatePettyCashReplenishmentCommandValidator _val = new();

    [Fact]
    public void Validator_Valid_Passes() => _val.TestValidate(Cmd()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("1404/05/10")]
    [InlineData("")]
    public void Validator_BadRegisterDate_Fails(string d)
        => _val.TestValidate(Cmd() with { RegisterDate = d }).ShouldHaveValidationErrorFor(x => x.RegisterDate);

    [Theory]
    [InlineData("404")]
    [InlineData("14040")]
    [InlineData("abcd")]
    public void Validator_BadYear_Fails(string y)
        => _val.TestValidate(Cmd() with { Year = y }).ShouldHaveValidationErrorFor(x => x.Year);

    [Fact]
    public void Validator_InvalidPaymentMethod_Fails()
        => _val.TestValidate(Cmd() with { PaymentMethod = (PettyCashPaymentMethod)99 }).ShouldHaveValidationErrorFor(x => x.PaymentMethod);

    [Fact]
    public void Validator_EmptyIds_Fail()
    {
        var r = _val.TestValidate(Cmd() with { FundId = Guid.Empty, SourceBankAccountId = Guid.Empty });
        r.ShouldHaveValidationErrorFor(x => x.FundId);
        r.ShouldHaveValidationErrorFor(x => x.SourceBankAccountId);
    }
}
