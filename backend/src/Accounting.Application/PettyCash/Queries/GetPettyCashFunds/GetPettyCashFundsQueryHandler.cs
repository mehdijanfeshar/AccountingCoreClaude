using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFunds;

public sealed class GetPettyCashFundsQueryHandler
    : IRequestHandler<GetPettyCashFundsQuery, IReadOnlyList<PettyCashFundDto>>
{
    private readonly IPettyCashFundReadRepository _readRepository;

    public GetPettyCashFundsQueryHandler(IPettyCashFundReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<IReadOnlyList<PettyCashFundDto>> Handle(GetPettyCashFundsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetAllAsync(request.VahedCode, cancellationToken);
}
