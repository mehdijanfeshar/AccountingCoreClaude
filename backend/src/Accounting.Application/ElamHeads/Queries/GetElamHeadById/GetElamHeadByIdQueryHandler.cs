using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ElamHeads.Queries.GetElamHeadById;

/// <summary>
/// Delegates straight to <see cref="IElamHeadReadRepository.GetByIdAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetElamHeadByIdQueryHandler : IRequestHandler<GetElamHeadByIdQuery, ElamHeadDto?>
{
    private readonly IElamHeadReadRepository _readRepository;

    public GetElamHeadByIdQueryHandler(IElamHeadReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<ElamHeadDto?> Handle(GetElamHeadByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
