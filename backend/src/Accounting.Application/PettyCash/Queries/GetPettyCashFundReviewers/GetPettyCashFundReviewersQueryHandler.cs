using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundReviewers;

public sealed class GetPettyCashFundReviewersQueryHandler
    : IRequestHandler<GetPettyCashFundReviewersQuery, IReadOnlyList<PettyCashFundReviewerDto>>
{
    private readonly IPettyCashFundReadRepository _pettyCashFundReadRepository;
    private readonly IPettyCashFundReviewerReadRepository _reviewerReadRepository;

    public GetPettyCashFundReviewersQueryHandler(
        IPettyCashFundReadRepository pettyCashFundReadRepository,
        IPettyCashFundReviewerReadRepository reviewerReadRepository)
    {
        _pettyCashFundReadRepository = pettyCashFundReadRepository;
        _reviewerReadRepository = reviewerReadRepository;
    }

    public async Task<IReadOnlyList<PettyCashFundReviewerDto>> Handle(
        GetPettyCashFundReviewersQuery request,
        CancellationToken cancellationToken)
    {
        var fund = await _pettyCashFundReadRepository.GetByIdAsync(request.FundId, request.VahedCode, cancellationToken);

        if (fund is null || fund.IsDeleted)
        {
            throw new NotFoundException("PettyCashFund", request.FundId);
        }

        return await _reviewerReadRepository.GetByFundIdAsync(request.FundId, request.VahedCode, cancellationToken);
    }
}
