using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashRefunds;

public sealed class GetPettyCashRefundsQueryHandler : IRequestHandler<GetPettyCashRefundsQuery, IReadOnlyList<PettyCashRefundDto>>
{
    private readonly IPettyCashRefundReadRepository _readRepository;

    public GetPettyCashRefundsQueryHandler(IPettyCashRefundReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<IReadOnlyList<PettyCashRefundDto>> Handle(GetPettyCashRefundsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByFundAsync(request.FundId, request.VahedCode, cancellationToken);
}
