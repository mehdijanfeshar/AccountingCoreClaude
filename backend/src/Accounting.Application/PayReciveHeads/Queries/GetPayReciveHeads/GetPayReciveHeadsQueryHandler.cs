using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PayReciveHeads.Queries.GetPayReciveHeads;

/// <summary>
/// Delegates straight to <see cref="IPayReciveHeadReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetPayReciveHeadsQueryHandler
    : IRequestHandler<GetPayReciveHeadsQuery, PagedResult<PayReciveHeadDto>>
{
    private readonly IPayReciveHeadReadRepository _readRepository;

    public GetPayReciveHeadsQueryHandler(IPayReciveHeadReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<PayReciveHeadDto>> Handle(
        GetPayReciveHeadsQuery request,
        CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
