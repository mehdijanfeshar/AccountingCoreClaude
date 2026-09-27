using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundReviewers;

public sealed class GetPettyCashFundReviewersQueryHandler
    : IRequestHandler<GetPettyCashFundReviewersQuery, IReadOnlyList<PettyCashFundReviewerDto>>
{
    private readonly IRevolvingFundReadRepository _revolvingFundReadRepository;
    private readonly IPettyCashFundReviewerReadRepository _reviewerReadRepository;

    public GetPettyCashFundReviewersQueryHandler(
        IRevolvingFundReadRepository revolvingFundReadRepository,
        IPettyCashFundReviewerReadRepository reviewerReadRepository)
    {
        _revolvingFundReadRepository = revolvingFundReadRepository;
        _reviewerReadRepository = reviewerReadRepository;
    }

    public async Task<IReadOnlyList<PettyCashFundReviewerDto>> Handle(
        GetPettyCashFundReviewersQuery request,
        CancellationToken cancellationToken)
    {
        var fund = await _revolvingFundReadRepository.GetByIdAsync(request.FundId, request.VahedCode, cancellationToken);

        if (fund is null || fund.IsDeleted == true)
        {
            throw new NotFoundException("RevolvingFund", request.FundId);
        }

        return await _reviewerReadRepository.GetByFundIdAsync(request.FundId, request.VahedCode, cancellationToken);
    }
}
