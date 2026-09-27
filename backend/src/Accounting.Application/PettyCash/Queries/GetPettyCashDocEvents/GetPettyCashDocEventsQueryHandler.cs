using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashDocEvents;

public sealed class GetPettyCashDocEventsQueryHandler
    : IRequestHandler<GetPettyCashDocEventsQuery, IReadOnlyList<PettyCashDocEventDto>>
{
    private readonly IPettyCashExpenseDocReadRepository _expenseDocReadRepository;
    private readonly IPettyCashDocEventReadRepository _eventReadRepository;

    public GetPettyCashDocEventsQueryHandler(
        IPettyCashExpenseDocReadRepository expenseDocReadRepository,
        IPettyCashDocEventReadRepository eventReadRepository)
    {
        _expenseDocReadRepository = expenseDocReadRepository;
        _eventReadRepository = eventReadRepository;
    }

    public async Task<IReadOnlyList<PettyCashDocEventDto>> Handle(GetPettyCashDocEventsQuery request, CancellationToken cancellationToken)
    {
        // Existence/ownership of the parent document — throws UnitAccessDeniedException (403)
        // for a cross-unit document; a missing one becomes NotFoundException (404) here rather
        // than silently returning an empty event list.
        var expenseDoc = await _expenseDocReadRepository.GetByIdAsync(request.ExpenseDocId, request.VahedCode, cancellationToken);

        if (expenseDoc is null)
        {
            throw new NotFoundException("PettyCashExpenseDoc", request.ExpenseDocId);
        }

        return await _eventReadRepository.GetByExpenseDocIdAsync(request.ExpenseDocId, request.VahedCode, cancellationToken);
    }
}
