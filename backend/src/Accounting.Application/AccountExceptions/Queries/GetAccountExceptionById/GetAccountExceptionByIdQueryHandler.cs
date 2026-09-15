using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AccountExceptions.Queries.GetAccountExceptionById;

/// <summary>
/// Delegates straight to <see cref="IAccountExceptionReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class GetAccountExceptionByIdQueryHandler : IRequestHandler<GetAccountExceptionByIdQuery, AccountExceptionDto?>
{
    private readonly IAccountExceptionReadRepository _readRepository;

    public GetAccountExceptionByIdQueryHandler(IAccountExceptionReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<AccountExceptionDto?> Handle(GetAccountExceptionByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
