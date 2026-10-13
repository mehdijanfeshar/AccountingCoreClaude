using Accounting.Application.Commands.PettyCash.Common;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Moq;

namespace Accounting.Application.Tests.PettyCash.Common.Authorizers;

public class PettyCashRefundRecorderAuthorizerTests
{
    private readonly Mock<IPettyCashFundReviewerRepository> _reviewers = new();
    private readonly Mock<ICurrentUser> _user = new();

    public PettyCashRefundRecorderAuthorizerTests() => _user.SetupGet(u => u.UserId).Returns("u1");

    private PettyCashRefundRecorderAuthorizer Sut() => new(_reviewers.Object, _user.Object);

    private static TB_PC_FUND Fund(PettyCashRefundRecorder rec, string custodian = "cust")
        => new() { ID = Guid.NewGuid(), REFUND_RECORDER = rec, CUSTODIAN_USERID = custodian };

    private void Roles(TB_PC_FUND f, params PettyCashRole[] roles)
        => _reviewers.Setup(r => r.GetActiveRolesAsync(f.ID, "u1", It.IsAny<CancellationToken>())).ReturnsAsync(roles.ToList());

    [Fact]
    public async Task Handle_CustodianRecorderAndCallerIsCustodian_Passes()
        => await Sut().EnsureCanRecordAsync(Fund(PettyCashRefundRecorder.Custodian, "u1"));

    [Fact]
    public async Task Handle_CustodianRecorderAndCallerNotCustodian_Throws()
        => await Assert.ThrowsAsync<PettyCashRefundRecorderMismatchException>(
            () => Sut().EnsureCanRecordAsync(Fund(PettyCashRefundRecorder.Custodian, "other")));

    [Theory]
    [InlineData(PettyCashRefundRecorder.Treasurer, PettyCashRole.Treasurer)]
    [InlineData(PettyCashRefundRecorder.SeniorAccountant, PettyCashRole.SeniorAccountant)]
    [InlineData(PettyCashRefundRecorder.FinanceManager, PettyCashRole.FinanceManager)]
    public async Task Handle_RoleRecorderWithRole_Passes(PettyCashRefundRecorder rec, PettyCashRole role)
    {
        var f = Fund(rec);
        Roles(f, role);

        await Sut().EnsureCanRecordAsync(f);
    }

    [Theory]
    [InlineData(PettyCashRefundRecorder.Treasurer)]
    [InlineData(PettyCashRefundRecorder.SeniorAccountant)]
    [InlineData(PettyCashRefundRecorder.FinanceManager)]
    public async Task Handle_RoleRecorderWithoutRole_Throws(PettyCashRefundRecorder rec)
    {
        var f = Fund(rec);
        Roles(f, PettyCashRole.Inspector);

        await Assert.ThrowsAsync<PettyCashRefundRecorderMismatchException>(() => Sut().EnsureCanRecordAsync(f));
    }
}
