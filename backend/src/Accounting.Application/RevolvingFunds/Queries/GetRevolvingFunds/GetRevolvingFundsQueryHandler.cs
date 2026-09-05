using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.RevolvingFunds.Queries.GetRevolvingFunds;

/// <summary>
/// Delegates straight to <see cref="IRevolvingFundReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetRevolvingFundsQueryHandler : IRequestHandler<GetRevolvingFundsQuery, PagedResult<RevolvingFundDto>>
{
    private readonly IRevolvingFundReadRepository _readRepository;

    public GetRevolvingFundsQueryHandler(IRevolvingFundReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<RevolvingFundDto>> Handle(GetRevolvingFundsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
