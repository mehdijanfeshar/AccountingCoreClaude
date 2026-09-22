using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Reports.MatrixReport.GetMatrixReport;

/// <summary>
/// Delegates straight to the read repository. Read-side handlers never touch
/// <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetMatrixReportQueryHandler
    : IRequestHandler<GetMatrixReportQuery, MatrixReportResultDto>
{
    private readonly IMatrixReportReadRepository _readRepository;

    public GetMatrixReportQueryHandler(IMatrixReportReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<MatrixReportResultDto> Handle(
        GetMatrixReportQuery request,
        CancellationToken cancellationToken)
        => _readRepository.GetAsync(request, cancellationToken);
}
