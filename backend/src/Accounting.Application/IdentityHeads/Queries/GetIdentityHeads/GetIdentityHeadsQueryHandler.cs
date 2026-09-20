using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.IdentityHeads.Queries.GetIdentityHeads;

/// <summary>
/// Delegates straight to <see cref="IIdentityHeadReadRepository.GetPagedAsync"/>. Read-side only;
/// no <c>IUnitOfWork</c> dependency anywhere in this feature.
/// </summary>
public sealed class GetIdentityHeadsQueryHandler : IRequestHandler<GetIdentityHeadsQuery, PagedResult<IdentityHeadDto>>
{
    private readonly IIdentityHeadReadRepository _readRepository;

    public GetIdentityHeadsQueryHandler(IIdentityHeadReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<IdentityHeadDto>> Handle(GetIdentityHeadsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.VahedCode,
            request.IdentityGroupId,
            request.Year,
            cancellationToken);
}
