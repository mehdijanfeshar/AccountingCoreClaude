using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.ChequeTypes.Queries.GetChequeTypeById;

/// <summary>
/// Delegates straight to <see cref="IChequeTypeReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetChequeTypeByIdQueryHandler : IRequestHandler<GetChequeTypeByIdQuery, ChequeTypeDto?>
{
    private readonly IChequeTypeReadRepository _readRepository;

    public GetChequeTypeByIdQueryHandler(IChequeTypeReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<ChequeTypeDto?> Handle(GetChequeTypeByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
