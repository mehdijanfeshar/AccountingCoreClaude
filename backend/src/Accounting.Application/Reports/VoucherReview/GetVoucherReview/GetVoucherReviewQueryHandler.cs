using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Reports.VoucherReview.GetVoucherReview;

/// <summary>
/// Delegates straight to the read repository. Read-side handlers never touch
/// <c>IUnitOfWork</c> — there is nothing to persist.
/// </summary>
public sealed class GetVoucherReviewQueryHandler
    : IRequestHandler<GetVoucherReviewQuery, VoucherReviewResultDto>
{
    private readonly IVoucherReviewReadRepository _readRepository;

    public GetVoucherReviewQueryHandler(IVoucherReviewReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<VoucherReviewResultDto> Handle(
        GetVoucherReviewQuery request,
        CancellationToken cancellationToken)
        => _readRepository.GetAsync(request, cancellationToken);
}
