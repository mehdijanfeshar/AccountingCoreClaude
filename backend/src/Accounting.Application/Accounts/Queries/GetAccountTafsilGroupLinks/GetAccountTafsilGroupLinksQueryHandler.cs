using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Accounts.Queries.GetAccountTafsilGroupLinks;

/// <summary>
/// Delegates straight to <see cref="IAccountCodeReadRepository.GetTafsilGroupLinksAsync"/>.
/// Read-side handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetAccountTafsilGroupLinksQueryHandler
    : IRequestHandler<GetAccountTafsilGroupLinksQuery, IReadOnlyList<AccountTafsilGroupLinkDto>>
{
    private readonly IAccountCodeReadRepository _readRepository;

    public GetAccountTafsilGroupLinksQueryHandler(IAccountCodeReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<IReadOnlyList<AccountTafsilGroupLinkDto>> Handle(
        GetAccountTafsilGroupLinksQuery request,
        CancellationToken cancellationToken)
        => _readRepository.GetTafsilGroupLinksAsync(request.AccountCodeId, cancellationToken);
}
