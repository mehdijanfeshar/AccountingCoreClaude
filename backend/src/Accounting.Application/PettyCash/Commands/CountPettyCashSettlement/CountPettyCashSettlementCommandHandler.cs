using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Commands.Common;
using MediatR;

namespace Accounting.Application.PettyCash.Commands.CountPettyCashSettlement;

/// <summary>
/// Loads/provisions the fund's current Draft settlement period
/// (<see cref="IPettyCashSettlementPeriodProvisioner.EnsureDraftAsync"/>), stamps
/// <c>COUNTED_BALANCE</c>, and saves — a single, simple write, no transaction boundary needed
/// beyond the one <see cref="IUnitOfWork.SaveChangesAsync"/> call (recording a count is not itself
/// an irreversible/multi-aggregate operation; that is <c>finalize</c>'s job).
/// </summary>
public sealed class CountPettyCashSettlementCommandHandler : IRequestHandler<CountPettyCashSettlementCommand, Guid>
{
    private readonly IPettyCashFundRepository _fundRepository;
    private readonly IPettyCashSettlementPeriodProvisioner _provisioner;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CountPettyCashSettlementCommandHandler(
        IPettyCashFundRepository fundRepository,
        IPettyCashSettlementPeriodProvisioner provisioner,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _fundRepository = fundRepository;
        _provisioner = provisioner;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CountPettyCashSettlementCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepository.GetForUpdateAsync(request.FundId, request.VahedCode, cancellationToken)
            ?? throw new NotFoundException("PettyCashFund", request.FundId);

        var period = await _provisioner.EnsureDraftAsync(fund, request.VahedCode, cancellationToken);

        period.COUNTED_BALANCE = request.CountedBalance;
        period.CHANGEUSERID = _currentUser.UserId;
        period.UPDATEDDATE = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return period.ID;
    }
}
