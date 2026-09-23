using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.VahedTypes.Queries.GetVahedTypes;

/// <summary>
/// Delegates straight to <see cref="IVahedTypeReadRepository.GetAllAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetVahedTypesQueryHandler : IRequestHandler<GetVahedTypesQuery, IReadOnlyList<VahedTypeDto>>
{
    private readonly IVahedTypeReadRepository _readRepository;

    public GetVahedTypesQueryHandler(IVahedTypeReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<IReadOnlyList<VahedTypeDto>> Handle(GetVahedTypesQuery request, CancellationToken cancellationToken)
        => _readRepository.GetAllAsync(cancellationToken);
}
