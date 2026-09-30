using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetTreasuryDashboard;

public sealed class GetTreasuryDashboardQueryHandler : IRequestHandler<GetTreasuryDashboardQuery, TreasuryDashboardDto>
{
    private readonly ITreasuryDashboardReadRepository _dashboardReadRepository;

    public GetTreasuryDashboardQueryHandler(ITreasuryDashboardReadRepository dashboardReadRepository)
    {
        _dashboardReadRepository = dashboardReadRepository;
    }

    public Task<TreasuryDashboardDto> Handle(GetTreasuryDashboardQuery request, CancellationToken cancellationToken)
        => _dashboardReadRepository.GetAsync(request.VahedCode, cancellationToken);
}
