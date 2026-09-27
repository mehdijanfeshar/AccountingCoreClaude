using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashExpenseDocs;

public sealed class GetPettyCashExpenseDocsQueryHandler
    : IRequestHandler<GetPettyCashExpenseDocsQuery, PettyCashExpenseDocListResult>
{
    private readonly IPettyCashExpenseDocReadRepository _readRepository;

    public GetPettyCashExpenseDocsQueryHandler(IPettyCashExpenseDocReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PettyCashExpenseDocListResult> Handle(GetPettyCashExpenseDocsQuery request, CancellationToken cancellationToken)
        => _readRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            new PettyCashExpenseDocFilter(request.FundId, request.State, request.States, request.Search),
            request.VahedCode,
            cancellationToken);
}
