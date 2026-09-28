using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundSettlements;

public sealed class GetPettyCashFundSettlementsQueryHandler
    : IRequestHandler<GetPettyCashFundSettlementsQuery, IReadOnlyList<PettyCashSettlementHistoryItemDto>>
{
    private readonly IPettyCashSettlementReadRepository _settlementReadRepository;

    public GetPettyCashFundSettlementsQueryHandler(IPettyCashSettlementReadRepository settlementReadRepository)
    {
        _settlementReadRepository = settlementReadRepository;
    }

    public Task<IReadOnlyList<PettyCashSettlementHistoryItemDto>> Handle(
        GetPettyCashFundSettlementsQuery request, CancellationToken cancellationToken)
        => _settlementReadRepository.GetFinalHistoryAsync(request.FundId, request.VahedCode, cancellationToken);
}
