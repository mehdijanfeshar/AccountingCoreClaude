using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.FinancialStatements.Queries.GetFsRun;

public sealed class GetFsRunQueryHandler : IRequestHandler<GetFsRunQuery, FsRunDetailDto?>
{
    private readonly IFsRunRepository _runRepository;

    public GetFsRunQueryHandler(IFsRunRepository runRepository)
    {
        _runRepository = runRepository;
    }

    public Task<FsRunDetailDto?> Handle(GetFsRunQuery request, CancellationToken cancellationToken)
        => _runRepository.GetDetailAsync(request.Id, request.VahedCode, cancellationToken);
}
