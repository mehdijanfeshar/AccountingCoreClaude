using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Reports.CrossTab.GetCrossTabReport;

/// <summary>
/// Delegates straight to the read repository. Read-side handlers never touch
/// <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetCrossTabReportQueryHandler
    : IRequestHandler<GetCrossTabReportQuery, CrossTabResultDto>
{
    private readonly ICrossTabReportReadRepository _readRepository;

    public GetCrossTabReportQueryHandler(ICrossTabReportReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<CrossTabResultDto> Handle(
        GetCrossTabReportQuery request,
        CancellationToken cancellationToken)
        => _readRepository.GetAsync(request, cancellationToken);
}
