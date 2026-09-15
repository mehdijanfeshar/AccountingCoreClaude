using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.WorkShops.Queries.GetWorkShopById;

/// <summary>
/// Delegates straight to <see cref="IWorkShopReadRepository.GetByIdAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetWorkShopByIdQueryHandler : IRequestHandler<GetWorkShopByIdQuery, WorkShopDto?>
{
    private readonly IWorkShopReadRepository _readRepository;

    public GetWorkShopByIdQueryHandler(IWorkShopReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<WorkShopDto?> Handle(GetWorkShopByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
