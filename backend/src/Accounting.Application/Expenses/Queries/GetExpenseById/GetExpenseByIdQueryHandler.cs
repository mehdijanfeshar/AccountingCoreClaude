using Accounting.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.Expenses.Queries.GetExpenseById;

/// <summary>
/// Delegates straight to <see cref="IExpenseReadRepository.GetByIdAsync"/>. Read-side handlers
/// never touch <see cref="IUnitOfWork"/> — there is nothing to persist.
/// </summary>
public sealed class GetExpenseByIdQueryHandler : IRequestHandler<GetExpenseByIdQuery, ExpenseDto?>
{
    private readonly IExpenseReadRepository _readRepository;

    public GetExpenseByIdQueryHandler(IExpenseReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<ExpenseDto?> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
        => _readRepository.GetByIdAsync(request.Id, cancellationToken);
}
