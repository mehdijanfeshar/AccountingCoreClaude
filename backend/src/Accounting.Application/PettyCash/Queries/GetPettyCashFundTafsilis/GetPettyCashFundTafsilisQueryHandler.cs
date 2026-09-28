using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundTafsilis;

public sealed class GetPettyCashFundTafsilisQueryHandler
    : IRequestHandler<GetPettyCashFundTafsilisQuery, IReadOnlyList<PettyCashSettlementTafsiliDto>>
{
    private readonly IPettyCashSettlementReadRepository _settlementReadRepository;

    public GetPettyCashFundTafsilisQueryHandler(IPettyCashSettlementReadRepository settlementReadRepository)
    {
        _settlementReadRepository = settlementReadRepository;
    }

    public Task<IReadOnlyList<PettyCashSettlementTafsiliDto>> Handle(
        GetPettyCashFundTafsilisQuery request, CancellationToken cancellationToken)
        => _settlementReadRepository.GetFundTafsilisAsync(request.FundId, request.VahedCode, cancellationToken);
}
