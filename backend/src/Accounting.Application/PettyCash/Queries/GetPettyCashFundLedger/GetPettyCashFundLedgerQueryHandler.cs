using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundLedger;

public sealed class GetPettyCashFundLedgerQueryHandler : IRequestHandler<GetPettyCashFundLedgerQuery, PettyCashFundLedgerDto?>
{
    private readonly IPettyCashLedgerReadRepository _readRepository;

    public GetPettyCashFundLedgerQueryHandler(IPettyCashLedgerReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PettyCashFundLedgerDto?> Handle(GetPettyCashFundLedgerQuery request, CancellationToken cancellationToken)
        => _readRepository.GetAsync(request.FundId, request.From, request.To, request.Type, request.VahedCode, cancellationToken);
}
