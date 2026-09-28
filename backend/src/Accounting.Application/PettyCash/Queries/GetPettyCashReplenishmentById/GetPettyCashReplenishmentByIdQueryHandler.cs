using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashReplenishmentById;

public sealed class GetPettyCashReplenishmentByIdQueryHandler
    : IRequestHandler<GetPettyCashReplenishmentByIdQuery, PettyCashReplenishmentDto?>
{
    private readonly IPettyCashReplenishmentReadRepository _readRepository;

    public GetPettyCashReplenishmentByIdQueryHandler(IPettyCashReplenishmentReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PettyCashReplenishmentDto?> Handle(GetPettyCashReplenishmentByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, request.VahedCode, cancellationToken);
}
