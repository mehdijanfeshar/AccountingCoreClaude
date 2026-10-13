using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Common.SubmitRuleChecker;

public class PettyCashSubmitRuleCheckerTests
{
    private readonly Mock<IPettyCashExpenseDocRepository> _docs = new();
    private readonly Mock<IPettyCashReplenishmentRepository> _repl = new();
    private readonly Mock<IPettyCashRefundRepository> _refund = new();
    private readonly Guid _docId = Guid.NewGuid();
    private readonly Guid _fundId = Guid.NewGuid();

    private PettyCashSubmitRuleChecker Sut(decimal approved = 0, decimal inFlight = 0, decimal paid = 0, decimal refund = 0)
    {
        _docs.Setup(d => d.GetFundExposureAsync(_fundId, "0101", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PettyCashFundExposure(approved, 0, inFlight, 0));
        _repl.Setup(r => r.GetPaidTotalAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(paid);
        _refund.Setup(r => r.GetTotalAsync(_fundId, "0101", It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        return new(_docs.Object, _repl.Object, _refund.Object);
    }

    private Task Check(PettyCashSubmitRuleChecker sut, string? date = "14040510", decimal total = 100,
        decimal ceiling = 1000, decimal perDoc = 500)
        => sut.EnsureSubmittableAsync(_docId, _fundId, ceiling, perDoc, date, "1404", total, "0101", null);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("14030510")]
    public async Task Handle_InvoiceDateOutsideYear_Throws(string? date)
        => await Assert.ThrowsAsync<PettyCashInvoiceYearMismatchException>(() => Check(Sut(), date));

    [Fact]
    public async Task Handle_OverPerDocLimit_Throws()
        => await Assert.ThrowsAsync<PettyCashPerDocLimitExceededException>(() => Check(Sut(), total: 501));

    [Fact]
    public async Task Handle_AmountExceedsCashBalance_Throws()
        // مانده نقد = ۱۰۰۰ - ۴۰۰ - ۵۰۰ = ۱۰۰ ؛ مبلغ ۲۰۰ بیشتر است
        => await Assert.ThrowsAsync<PettyCashInsufficientCashBalanceException>(
            () => Check(Sut(approved: 400, inFlight: 500), total: 200));

    [Fact]
    public async Task Handle_AmountEqualsCashBalance_Passes()
        => await Check(Sut(approved: 400, inFlight: 500), total: 100);

    [Fact]
    public async Task Handle_ReplenishmentAndRefundIncreaseCashBalance_Passes()
        // ۱۰۰۰ - ۹۰۰ + ۳۰۰ + ۱۰۰ = ۵۰۰
        => await Check(Sut(approved: 400, inFlight: 500, paid: 300, refund: 100), total: 500);

    [Fact]
    public async Task Handle_PerDocLimitCheckedBeforeBalance()
        => await Assert.ThrowsAsync<PettyCashPerDocLimitExceededException>(
            () => Check(Sut(approved: 1000), total: 600));
}
