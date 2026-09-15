using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.VahedInfos.Queries.GetVahedInfoById;

/// <summary>
/// Delegates straight to <see cref="IVahedInfoReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist. Returns
/// <see langword="null"/> when the repository finds no matching row; never throws a
/// not-found exception.
/// </summary>
public sealed class GetVahedInfoByIdQueryHandler : IRequestHandler<GetVahedInfoByIdQuery, VahedInfoDto?>
{
    private readonly IVahedInfoReadRepository _readRepository;

    public GetVahedInfoByIdQueryHandler(IVahedInfoReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<VahedInfoDto?> Handle(GetVahedInfoByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
