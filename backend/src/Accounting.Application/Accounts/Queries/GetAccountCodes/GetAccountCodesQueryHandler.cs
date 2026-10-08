using Accounting.Application.Common;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Accounts.Queries.GetAccountCodes;

/// <summary>
/// Delegates straight to <see cref="IAccountCodeReadRepository.GetPagedAsync"/>. Read-side
/// handlers never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetAccountCodesQueryHandler : IRequestHandler<GetAccountCodesQuery, PagedResult<AccountCodeDto>>
{
    private readonly IAccountCodeReadRepository _readRepository;

    public GetAccountCodesQueryHandler(IAccountCodeReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PagedResult<AccountCodeDto>> Handle(GetAccountCodesQuery request, CancellationToken cancellationToken)
    {
        var search = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : Accounting.Application.FinancialStatements.Engine.FsText.NormalizeDigits(request.Search).Trim();

        return search is null
            ? _readRepository.GetPagedAsync(request.PageNumber, request.PageSize, cancellationToken)
            : _readRepository.SearchPagedAsync(search, request.PageNumber, request.PageSize, cancellationToken);
    }
}
