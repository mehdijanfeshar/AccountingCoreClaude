using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Tafsilis.Queries.GetTafsiliById;

/// <summary>
/// Delegates straight to <see cref="ITafsiliReadRepository.GetByIdAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetTafsiliByIdQueryHandler : IRequestHandler<GetTafsiliByIdQuery, TafsiliDto?>
{
    private readonly ITafsiliReadRepository _readRepository;

    public GetTafsiliByIdQueryHandler(ITafsiliReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<TafsiliDto?> Handle(GetTafsiliByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
