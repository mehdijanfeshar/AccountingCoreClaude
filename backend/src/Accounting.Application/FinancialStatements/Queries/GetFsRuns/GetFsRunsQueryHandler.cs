using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.GetFsRuns;

public sealed class GetFsRunsQueryHandler : IRequestHandler<GetFsRunsQuery, IReadOnlyList<FsRunSummaryDto>>
{
    private readonly IFsRunRepository _runRepository;

    public GetFsRunsQueryHandler(IFsRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public Task<IReadOnlyList<FsRunSummaryDto>> Handle(GetFsRunsQuery request, CancellationToken cancellationToken)
        => _runRepository.ListAsync(request.VahedCode, string.IsNullOrWhiteSpace(request.Year) ? null : request.Year.Trim(), cancellationToken);
}
