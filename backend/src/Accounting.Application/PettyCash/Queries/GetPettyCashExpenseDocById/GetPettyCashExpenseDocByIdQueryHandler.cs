using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashExpenseDocById;

public sealed class GetPettyCashExpenseDocByIdQueryHandler
    : IRequestHandler<GetPettyCashExpenseDocByIdQuery, PettyCashExpenseDocDto?>
{
    private readonly IPettyCashExpenseDocReadRepository _readRepository;

    public GetPettyCashExpenseDocByIdQueryHandler(IPettyCashExpenseDocReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<PettyCashExpenseDocDto?> Handle(GetPettyCashExpenseDocByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, request.VahedCode, cancellationToken);
}
