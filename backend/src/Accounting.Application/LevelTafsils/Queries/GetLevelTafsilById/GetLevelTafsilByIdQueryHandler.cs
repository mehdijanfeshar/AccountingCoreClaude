using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.LevelTafsils.Queries.GetLevelTafsilById;

/// <summary>
/// Delegates straight to <see cref="ILevelTafsilReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetLevelTafsilByIdQueryHandler : IRequestHandler<GetLevelTafsilByIdQuery, LevelTafsilDto?>
{
    private readonly ILevelTafsilReadRepository _readRepository;

    public GetLevelTafsilByIdQueryHandler(ILevelTafsilReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<LevelTafsilDto?> Handle(GetLevelTafsilByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
