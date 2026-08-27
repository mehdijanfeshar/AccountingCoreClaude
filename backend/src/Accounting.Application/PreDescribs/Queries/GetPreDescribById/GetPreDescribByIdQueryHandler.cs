using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PreDescribs.Queries.GetPreDescribById;

/// <summary>
/// Delegates straight to <see cref="IPreDescribReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist. Returns
/// <see langword="null"/> when the repository finds no matching row; never throws a
/// not-found exception.
/// </summary>
public sealed class GetPreDescribByIdQueryHandler : IRequestHandler<GetPreDescribByIdQuery, PreDescribDto?>
{
    private readonly IPreDescribReadRepository _readRepository;

    public GetPreDescribByIdQueryHandler(IPreDescribReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PreDescribDto?> Handle(GetPreDescribByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
