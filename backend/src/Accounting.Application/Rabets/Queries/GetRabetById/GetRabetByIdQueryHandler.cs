using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Rabets.Queries.GetRabetById;

/// <summary>
/// Delegates straight to <see cref="IRabetReadRepository.GetByIdAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetRabetByIdQueryHandler : IRequestHandler<GetRabetByIdQuery, RabetDto?>
{
    private readonly IRabetReadRepository _readRepository;

    public GetRabetByIdQueryHandler(IRabetReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<RabetDto?> Handle(GetRabetByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
