using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashReplenishments;

public sealed class GetPettyCashReplenishmentsQueryHandler
    : IRequestHandler<GetPettyCashReplenishmentsQuery, PagedResult<PettyCashReplenishmentListItemDto>>
{
    private readonly IPettyCashReplenishmentReadRepository _readRepository;

    public GetPettyCashReplenishmentsQueryHandler(IPettyCashReplenishmentReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<PettyCashReplenishmentListItemDto>> Handle(
        GetPettyCashReplenishmentsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(
            request.PageNumber, request.PageSize, request.FundId, request.State, request.VahedCode, cancellationToken);
}
