using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.AccountExceptions.Queries.GetAccountExceptions;

/// <summary>
/// Delegates straight to <see cref="IAccountExceptionReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class GetAccountExceptionsQueryHandler : IRequestHandler<GetAccountExceptionsQuery, PagedResult<AccountExceptionDto>>
{
    private readonly IAccountExceptionReadRepository _readRepository;

    public GetAccountExceptionsQueryHandler(IAccountExceptionReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<AccountExceptionDto>> Handle(GetAccountExceptionsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken);
}
