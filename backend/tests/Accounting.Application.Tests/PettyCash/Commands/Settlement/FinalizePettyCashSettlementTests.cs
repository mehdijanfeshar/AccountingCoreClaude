using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Commands.PettyCash.FinalizePettyCashSettlement;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Queries.PettyCash;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Commands.Settlement;

/// <summary>نهایی‌سازی تسویه دوره تنخواه: نقش، SoD، انتقال در راه، شمارش و تراز.</summary>
public class FinalizePettyCashSettlementTests
{
    private readonly Mock<IPettyCashFundRepository> _funds = new();
    private readonly Mock<IPettyCashFundReviewerRepository> _reviewers = new();
    private readonly Mock<IPettyCashSettlementPeriodProvisioner> _prov = new();
    private readonly Mock<IPettyCashSettlementReadRepository> _read = new();
    private readonly Mock<IPettyCashExpenseDocRepository> _docs = new();
    private readonly Mock<IPettyCashDocEventRepository> _events = new();
    private readonly Mock<IPettyCashSettlementVoucherBuilder> _builder = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _user = new();
    private readonly Mock<IClientInfoProvider> _client = new();
    private readonly Guid _fundId = Guid.NewGuid();
    private readonly TB_PC_FUND _fund;
    private readonly TB_PC_SETTLEMENT_PERIOD _period;
    private readonly TB_PC_EXPENSE_DOC _doc;

    public FinalizePettyCashSettlementTests()
    {
        _user.SetupGet(u => u.UserId).Returns("acc1");
        _client.SetupGet(c => c.ClientIp).Returns("10.0.0.5");
        _fund = new TB_PC_FUND { ID = _fundId };
        _period = new TB_PC_SETTLEMENT_PERIOD
        {
            ID = Guid.NewGuid(), FUND_ID = _fundId, PERIOD_START = "14040101", PERIOD_END = "14040131",
            OPENING_BALANCE = 100m, COUNTED_BALANCE = 150m, STATE = PettyCashSettlementState.Draft,
        };
        _doc = new TB_PC_EXPENSE_DOC { ID = Guid.NewGuid(), ADDUSERID = "someone-else", YEAR = "1404", DOC_STATE = PettyCashDocState.Approved };

        _funds.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(_fund);
        _reviewers.Setup(r => r.GetActiveRolesAsync(_fundId, "acc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PettyCashRole> { PettyCashRole.SeniorAccountant });
        _prov.Setup(p => p.EnsureDraftAsync(_fund, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(_period);
        // موجودی اختتام: 100 + 200 - 150 = 150
        _read.Setup(r => r.GetPeriodMovementAsync(_fundId, "0101", "14040101", "14040131", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashSettlementMovementDto(200m, new List<PettyCashSettlementExpenseGroupDto>(), 150m, new List<Guid> { _doc.ID }));
        _docs.Setup(d => d.GetManyForUpdateAsync(It.IsAny<IReadOnlyList<Guid>>(), "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TB_PC_EXPENSE_DOC> { _doc });
        _docs.Setup(d => d.GetFundExposureAsync(_fundId, "0101", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashFundExposure(0m, 0, 0m, 0));
        _builder.Setup(b => b.BuildAndStageAsync(_fund, _period, It.IsAny<IReadOnlyList<PettyCashSettlementExpenseGroupDto>>(), "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashSettlementVoucherResult(Guid.NewGuid(), "77", 150m));
    }

    private FinalizePettyCashSettlementCommandHandler Sut()
        => new(_funds.Object, _reviewers.Object, _prov.Object, _read.Object, _docs.Object, _events.Object,
            _builder.Object, _uow.Object, _user.Object, _client.Object);

    private FinalizePettyCashSettlementCommand Cmd(bool ack = false) => new(_fundId, ack) { VahedCode = "0101" };

    private void NoSideEffects()
    {
        _builder.Verify(b => b.BuildAndStageAsync(It.IsAny<TB_PC_FUND>(), It.IsAny<TB_PC_SETTLEMENT_PERIOD>(),
            It.IsAny<IReadOnlyList<PettyCashSettlementExpenseGroupDto>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(PettyCashSettlementState.Draft, _period.STATE);
    }

    [Fact]
    public async Task Finalize_FundMissing_ThrowsNotFound()
    {
        _funds.Setup(f => f.GetForUpdateAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync((TB_PC_FUND?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => Sut().Handle(Cmd(), default));
    }

    [Theory]
    [InlineData(PettyCashRole.FinanceManager)]
    [InlineData(PettyCashRole.Treasurer)]
    [InlineData(PettyCashRole.Inspector)]
    public async Task Finalize_NotSeniorAccountant_ThrowsRoleRequired(PettyCashRole role)
    {
        _reviewers.Setup(r => r.GetActiveRolesAsync(_fundId, "acc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PettyCashRole> { role });
        await Assert.ThrowsAsync<PettyCashSettlementRoleRequiredException>(() => Sut().Handle(Cmd(), default));
        NoSideEffects();
    }

    [Fact]
    public async Task Finalize_NoRoles_ThrowsRoleRequired()
    {
        _reviewers.Setup(r => r.GetActiveRolesAsync(_fundId, "acc1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PettyCashRole>());
        await Assert.ThrowsAsync<PettyCashSettlementRoleRequiredException>(() => Sut().Handle(Cmd(), default));
    }

    [Fact]
    public async Task Finalize_NoDocuments_Throws()
    {
        _read.Setup(r => r.GetPeriodMovementAsync(_fundId, "0101", "14040101", "14040131", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashSettlementMovementDto(0m, new List<PettyCashSettlementExpenseGroupDto>(), 0m, new List<Guid>()));
        await Assert.ThrowsAsync<PettyCashSettlementNoDocumentsException>(() => Sut().Handle(Cmd(), default));
        NoSideEffects();
    }

    [Fact]
    public async Task Finalize_CallerCreatedOneOfTheDocs_ThrowsSoD()
    {
        _doc.ADDUSERID = "acc1";
        await Assert.ThrowsAsync<PettyCashSettlementSoDConflictException>(() => Sut().Handle(Cmd(), default));
        NoSideEffects();
        Assert.Equal(PettyCashDocState.Approved, _doc.DOC_STATE);
    }

    [Fact]
    public async Task Finalize_InFlightWithoutAcknowledge_Throws()
    {
        _docs.Setup(d => d.GetFundExposureAsync(_fundId, "0101", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashFundExposure(0m, 0, 40m, 2));
        await Assert.ThrowsAsync<PettyCashSettlementInFlightAcknowledgeRequiredException>(() => Sut().Handle(Cmd(ack: false), default));
        NoSideEffects();
    }

    [Fact]
    public async Task Finalize_InFlightWithAcknowledge_Proceeds()
    {
        _docs.Setup(d => d.GetFundExposureAsync(_fundId, "0101", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashFundExposure(0m, 0, 40m, 2));
        var res = await Sut().Handle(Cmd(ack: true), default);
        Assert.Equal(1, res.SettledDocCount);
    }

    [Fact]
    public async Task Finalize_CountedBalanceMissing_Throws()
    {
        _period.COUNTED_BALANCE = null;
        await Assert.ThrowsAsync<PettyCashSettlementCountedBalanceRequiredException>(() => Sut().Handle(Cmd(), default));
        NoSideEffects();
    }

    [Fact]
    public async Task Finalize_CountedBalanceMismatch_Throws()
    {
        _period.COUNTED_BALANCE = 149m;
        await Assert.ThrowsAsync<PettyCashSettlementCountedBalanceMismatchException>(() => Sut().Handle(Cmd(), default));
        NoSideEffects();
    }

    [Fact]
    public async Task Finalize_Valid_SettlesDocsWritesEventsAndCommits()
    {
        var vhId = Guid.NewGuid();
        _builder.Setup(b => b.BuildAndStageAsync(_fund, _period, It.IsAny<IReadOnlyList<PettyCashSettlementExpenseGroupDto>>(), "0101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashSettlementVoucherResult(vhId, "77", 150m));
        TB_PC_DOC_EVENT? ev = null;
        _events.Setup(e => e.AddAsync(It.IsAny<TB_PC_DOC_EVENT>(), It.IsAny<CancellationToken>()))
            .Callback<TB_PC_DOC_EVENT, CancellationToken>((x, _) => ev = x).Returns(Task.CompletedTask);

        var res = await Sut().Handle(Cmd(), default);

        Assert.Equal(PettyCashDocState.Settled, _doc.DOC_STATE);
        Assert.Equal(PettyCashSettlementState.Final, _period.STATE);
        Assert.Equal(vhId, _period.VOUCHERSHEAD_ID);
        Assert.Equal("acc1", _period.FINALIZED_BY_USERID);
        Assert.NotNull(_period.FINALIZED_DATE);
        Assert.Equal(new FinalizePettyCashSettlementResult(_period.ID, vhId, "77", 1), res);
        Assert.NotNull(ev);
        Assert.Equal(PettyCashDocAction.Settle, ev!.ACTION);
        Assert.Equal(PettyCashDocState.Approved, ev.FROM_STATE);
        Assert.Equal(PettyCashDocState.Settled, ev.TO_STATE);
        Assert.Equal("0101", ev.VAHEDCODE);
        Assert.Equal("10.0.0.5", ev.CLIENT_IP);
        Assert.Equal("1404", ev.YEAR);
        Assert.Contains("77", ev.NOTE);
        _uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Finalize_BuilderFails_RollsBackAndRethrows()
    {
        _builder.Setup(b => b.BuildAndStageAsync(It.IsAny<TB_PC_FUND>(), It.IsAny<TB_PC_SETTLEMENT_PERIOD>(),
                It.IsAny<IReadOnlyList<PettyCashSettlementExpenseGroupDto>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PettyCashSettlementFundAccountMissingException(_fundId));

        await Assert.ThrowsAsync<PettyCashSettlementFundAccountMissingException>(() => Sut().Handle(Cmd(), default));

        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(PettyCashSettlementState.Draft, _period.STATE);
    }

    [Fact]
    public void Validator_EmptyFundId_Fails()
    {
        var v = new FinalizePettyCashSettlementCommandValidator();
        v.TestValidate(new FinalizePettyCashSettlementCommand(Guid.Empty, false) { VahedCode = "0101" }).ShouldHaveValidationErrorFor(x => x.FundId);
        v.TestValidate(Cmd()).ShouldNotHaveAnyValidationErrors();
    }
}
