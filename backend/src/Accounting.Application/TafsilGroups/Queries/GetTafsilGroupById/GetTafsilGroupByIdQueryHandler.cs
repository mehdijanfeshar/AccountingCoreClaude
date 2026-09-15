using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.TafsilGroups.Queries.GetTafsilGroupById;

/// <summary>
/// Delegates straight to <see cref="ITafsilGroupReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetTafsilGroupByIdQueryHandler : IRequestHandler<GetTafsilGroupByIdQuery, TafsilGroupDto?>
{
    private readonly ITafsilGroupReadRepository _readRepository;

    public GetTafsilGroupByIdQueryHandler(ITafsilGroupReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<TafsilGroupDto?> Handle(GetTafsilGroupByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
