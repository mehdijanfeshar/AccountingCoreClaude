using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Accounts.Queries.GetAccountCodeById;

/// <summary>
/// Delegates straight to <see cref="IAccountCodeReadRepository.GetByIdAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist. Returns
/// <see langword="null"/> when the repository finds no matching row; never throws a
/// not-found exception (no such exception type exists yet in this project).
/// </summary>
public sealed class GetAccountCodeByIdQueryHandler : IRequestHandler<GetAccountCodeByIdQuery, AccountCodeDto?>
{
    private readonly IAccountCodeReadRepository _readRepository;

    public GetAccountCodeByIdQueryHandler(IAccountCodeReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<AccountCodeDto?> Handle(GetAccountCodeByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
