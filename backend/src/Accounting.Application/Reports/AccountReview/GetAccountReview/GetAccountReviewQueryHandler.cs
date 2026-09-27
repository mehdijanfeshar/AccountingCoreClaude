using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Reports.AccountReview.GetAccountReview;

/// <summary>
/// Delegates straight to the read repository. Read-side handlers never touch
/// <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetAccountReviewQueryHandler
    : IRequestHandler<GetAccountReviewQuery, AccountReviewResultDto>
{
    private readonly IAccountReviewReadRepository _readRepository;

    public GetAccountReviewQueryHandler(IAccountReviewReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<AccountReviewResultDto> Handle(
        GetAccountReviewQuery request,
        CancellationToken cancellationToken)
        => _readRepository.GetAsync(request, cancellationToken);
}
