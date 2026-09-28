using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

public sealed class PettyCashRefundRecorderAuthorizer : IPettyCashRefundRecorderAuthorizer
{
    private readonly IPettyCashFundReviewerRepository _reviewerRepository;
    private readonly ICurrentUser _currentUser;

    public PettyCashRefundRecorderAuthorizer(
        IPettyCashFundReviewerRepository reviewerRepository,
        ICurrentUser currentUser)
    {
        _reviewerRepository = reviewerRepository;
        _currentUser = currentUser;
    }

    public async Task EnsureCanRecordAsync(TB_PC_FUND fund, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;

        if (fund.REFUND_RECORDER == PettyCashRefundRecorder.Custodian)
        {
            if (!string.Equals(userId, fund.CUSTODIAN_USERID, StringComparison.Ordinal))
            {
                throw new PettyCashRefundRecorderMismatchException(fund.ID, fund.REFUND_RECORDER);
            }

            return;
        }

        var requiredRole = ToReviewerRole(fund.REFUND_RECORDER);
        var activeRoles = await _reviewerRepository.GetActiveRolesAsync(fund.ID, userId, cancellationToken);

        if (!activeRoles.Contains(requiredRole))
        {
            throw new PettyCashRefundRecorderMismatchException(fund.ID, fund.REFUND_RECORDER);
        }
    }

    private static PettyCashRole ToReviewerRole(PettyCashRefundRecorder recorder) => recorder switch
    {
        PettyCashRefundRecorder.Treasurer => PettyCashRole.Treasurer,
        PettyCashRefundRecorder.SeniorAccountant => PettyCashRole.SeniorAccountant,
        PettyCashRefundRecorder.FinanceManager => PettyCashRole.FinanceManager,
        _ => throw new InvalidOperationException($"No TB_PC_REVIEWER role mapping for refund recorder {recorder}."),
    };
}
