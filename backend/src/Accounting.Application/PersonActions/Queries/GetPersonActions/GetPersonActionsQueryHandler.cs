using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PersonActions.Queries.GetPersonActions;

/// <summary>
/// Delegates straight to <see cref="IPersonActionReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class GetPersonActionsQueryHandler : IRequestHandler<GetPersonActionsQuery, PagedResult<PersonActionDto>>
{
    private readonly IPersonActionReadRepository _readRepository;

    public GetPersonActionsQueryHandler(IPersonActionReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<PersonActionDto>> Handle(GetPersonActionsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
